# Research: Live Environment Monitoring

**Branch**: `copilot/add-live-monitoring-feature`  
**Date**: 2026-04-25  
**Purpose**: Consolidate technology decisions, best practices, and rationale for the live monitoring implementation plan.

---

## 1. NATS HTTP Monitoring API

**Decision**: Use the NATS built-in HTTP monitoring API (separate port, default 8222) for metric polling.

**Rationale**: NATS exposes a read-only HTTP monitoring API that provides rich server statistics without requiring an authenticated NATS connection. This is the canonical way to get server-level telemetry from NATS.

**Key endpoints used**:

| Endpoint | Data | Used For |
|----------|------|----------|
| `/varz` | Server version, connections, in/out msgs & bytes, uptime, mem usage | Core server metrics graph |
| `/jsz` | JetStream stream/consumer counts, total messages & bytes | JetStream summary card |
| `/healthz` | Operational health status (ok/error) | Health indicator badge |

**Key `/varz` fields** (from NATS docs):
```json
{
  "server_id": "...",
  "version": "2.10.x",
  "connections": 42,
  "total_connections": 1500,
  "in_msgs": 100000,
  "out_msgs": 98000,
  "in_bytes": 5242880,
  "out_bytes": 4194304,
  "uptime": "1d2h",
  "mem": 12582912,
  "max_connections": 65536
}
```

**Key `/jsz` fields**:
```json
{
  "config": { "max_memory": 1073741824 },
  "streams": 5,
  "consumers": 12,
  "messages": 50000,
  "bytes": 2097152
}
```

**Rate derivation**: NATS reports cumulative counters, not rates. The backend computes per-second rates from consecutive snapshot deltas: `rate = (current - previous) / intervalSeconds`.

**Alternatives considered**:
- **NATS system subjects (`$SYS.REQ.SERVER.*`)**: Requires an authenticated NATS connection and operator-level permissions. The monitoring HTTP API is simpler and read-only. Rejected for monitoring data.
- **NATS.Net client `GetServerInfoAsync` (already in CoreNatsAdapter)**: Returns the static `INFO` frame from the NATS protocol connection. Lacks cumulative counters for rate calculation. Unsuitable for time-series monitoring.

---

## 2. Real-Time Push: ASP.NET Core SignalR

**Decision**: Use ASP.NET Core SignalR (built-in to .NET SDK) for real-time metric push to the frontend.

**Rationale**: SignalR is the established real-time communication library for ASP.NET Core. It handles WebSocket transport with automatic fallback to Server-Sent Events and long-polling. It is already part of the .NET SDK — no additional NuGet package is required for the server side.

**Hub design**:
- Hub class: `MonitoringHub` at route `/hubs/monitoring`
- Group-per-environment pattern: clients join `$"env-{environmentId}"` on connect
- Server-to-client method: `ReceiveMonitoringSnapshot(MonitoringSnapshot snapshot)`
- Client-to-server method: `SubscribeToEnvironment(string environmentId)`

**Authentication**: SignalR connections use cookie-based session auth (same as REST API). The hub requires `[Authorize]`.

**Key implementation patterns**:
- Use `IHubContext<MonitoringHub>` (injected into `BackgroundService`) to push snapshots from outside the hub lifecycle.
- SignalR connections are tracked automatically; no manual connection registry needed.
- For the CORS case (SPA on a different origin), `AllowCredentials()` is already configured in CORS policy — SignalR needs the same.

**Alternatives considered**:
- **Server-Sent Events (SSE)**: Simpler, unidirectional. Rejected because the problem statement explicitly names SignalR, and SignalR's group pattern better supports multi-environment subscriptions.
- **Raw WebSockets**: More control but more boilerplate; SignalR's hub abstraction is a better fit for this use case.
- **gRPC streaming**: Over-engineered for this use case; requires proto file and code gen.

---

## 3. Frontend SignalR Client: `@microsoft/signalr`

**Decision**: Add `@microsoft/signalr` npm package to the frontend.

**Rationale**: This is the official Microsoft SignalR JavaScript/TypeScript client. It is not currently a dependency and must be added.

**Key patterns**:
```typescript
const connection = new HubConnectionBuilder()
  .withUrl('/hubs/monitoring', { withCredentials: true })
  .withAutomaticReconnect()
  .build();

connection.on('ReceiveMonitoringSnapshot', (snapshot: MonitoringSnapshot) => {
  // update state
});

await connection.start();
await connection.invoke('SubscribeToEnvironment', environmentId);
```

**React integration**: A custom hook `useMonitoringHub(environmentId)` manages connection lifecycle (start on mount, stop on unmount, handle reconnection state).

**Security note**: The `withCredentials: true` flag ensures the session cookie is sent on the WebSocket upgrade handshake.

**Version**: `@microsoft/signalr@8.x` (latest stable for .NET 8/9/10 SignalR protocol v2).

---

## 4. In-Memory Metric Store (Ring Buffer)

**Decision**: Use a per-environment in-memory circular buffer (`FixedSizeQueue<MonitoringSnapshot>`) backed by a `ConcurrentDictionary<Guid, FixedSizeQueue<MonitoringSnapshot>>` keyed by environment ID.

**Rationale**: Metric history is ephemeral (no persistence requirement). A ring buffer bounds memory automatically — no manual eviction logic. `ConcurrentDictionary` allows lock-free reads from the SignalR hub while the background service writes.

**Capacity**: 120 snapshots × ~500 bytes per snapshot × 10 environments ≈ 600 KB — well within the 5 MB/hour memory growth limit.

**Implementation**: `Queue<T>` with a max-size wrapper, or `System.Collections.Generic.Queue<T>` plus size check. Thread-safety via a `ReaderWriterLockSlim` per environment bucket, or `Channel<T>` producer/consumer.

**Alternatives considered**:
- **SQLite persistence**: Rejected — FR-MON-015 explicitly forbids persisting monitoring data. Also adds unnecessary I/O overhead for high-frequency metric writes.
- **Redis / MemoryCache**: Overkill for single-node deployment; adds external dependency.

---

## 5. Configurable Polling Interval

**Decision**: Two-tier configuration: global default in `appsettings.json`, per-environment override in the database (nullable int on `Environment`).

**Rationale**: Most environments share the same polling cadence (global default). Individual environments with special needs (busy production vs. quiet dev) get overrides.

**Global config** (`appsettings.json`):
```json
"Monitoring": {
  "DefaultPollingIntervalSeconds": 30,
  "MaxSnapshotsPerEnvironment": 120,
  "HttpTimeoutSeconds": 10
}
```

**Per-environment override**: New `MonitoringPollingIntervalSeconds` (nullable int) column on `Environment` table. Administered via environment update API.

**Runtime effect**: The `MonitoringPoller` background service reads the resolved interval each cycle. Changes to the per-environment value in the database are picked up on the next cycle (no restart needed).

**Constraints**: Minimum 5 s, maximum 300 s (5 min) — enforced by FluentValidation.

**Alternatives considered**:
- **Per-environment config file only**: Rejected — the database-backed per-environment setting integrates naturally with the existing environment management UI.

---

## 6. Monitoring URL per Environment

**Decision**: Add `MonitoringUrl` (nullable string) to the `Environment` domain entity and database table via EF Core migration.

**Rationale**: NATS monitoring HTTP API listens on a different port (8222) than the NATS protocol port (4222). Not all NATS deployments enable or expose the monitoring endpoint. Making it optional allows existing environments to continue working without modification.

**Validation**: If set, must be a valid `http://` or `https://` URL. Validated with FluentValidation on create/update.

**Database**: New nullable column `MonitoringUrl` on `Environments` table, added via EF Core migration.

**Alternatives considered**:
- **Derive from ServerUrl by port-swapping**: Fragile — assumes monitoring is always on 8222 and the host is reachable on that port. Rejected in favour of explicit configuration.

---

## 7. Recharts for Time-Series Graphs

**Decision**: Use `recharts` (already a project dependency at 3.8.1) for the monitoring graphs.

**Rationale**: Recharts is already bundled. The `<LineChart>` component with `<Line>`, `<XAxis>`, `<YAxis>`, `<Tooltip>`, and `<Legend>` primitives is sufficient for time-series display. It integrates well with Mantine's layout system.

**Chart types**:
- **Server metrics**: A single `<LineChart>` with multiple `<Line>` series (connections, inMsgsPerSec, outMsgsPerSec) sharing a time axis.
- **Byte rate**: Separate `<LineChart>` for inBytesPerSec and outBytesPerSec (different scale from message counts).
- **JetStream trend**: `<AreaChart>` for totalMessages growth over time.

**Performance**: With max 120 data points per series and 3–4 series per chart, Recharts renders well within the 200 ms list-render budget.

**Alternatives considered**:
- **D3.js directly**: More control but much more boilerplate. Recharts' React abstraction is sufficient for this use case.
- **Chart.js / react-chartjs-2**: Not in the project; adding it would duplicate Recharts' capabilities.

---

## 8. Module Placement: `Monitoring` Bounded Context

**Decision**: Add a new `Monitoring` module within the existing Clean Architecture layer structure, following the same pattern as existing modules (Dashboard, CoreNats, etc.).

**Files added per layer**:

| Layer | New files |
|-------|-----------|
| Domain | None — this feature does not introduce new aggregates/value objects; monitoring contracts and read models live in Application. |
| Application | `Monitoring/Ports/IMonitoringAdapter.cs`, `Monitoring/Ports/IMonitoringMetricsStore.cs`, `Monitoring/Queries/GetMonitoringHistoryQuery.cs`, `Monitoring/Models/MonitoringModels.cs` |
| Infrastructure | `Nats/NatsMonitoringHttpAdapter.cs`, `Monitoring/MonitoringMetricsStore.cs` |
| Web | `BackgroundServices/MonitoringPoller.cs`, `Hubs/MonitoringHub.cs`, `Endpoints/MonitoringEndpoints.cs` |
| Frontend | `src/features/monitoring/` (MonitoringPage, hooks, types, components) |

**Rationale**: Consistent with the existing 9 bounded context modules. A new module avoids polluting the Dashboard module (which is a read-once summary) with continuous real-time data. Placing monitoring ports and read models in Application preserves the intended Clean Architecture boundary: Domain remains focused on core business concepts, while monitoring data retrieval and history access stay as application-level contracts.

---

## 9. Security Considerations

- The `MonitoringHub` requires `[Authorize]` — same session-cookie auth as all REST endpoints.
- The NATS monitoring HTTP API is polled from the **backend server**, not from the browser. Browser clients never make direct requests to the NATS monitoring port.
- `MonitoringUrl` is an admin-only field (only users with `Administrator` role can set/change it), preventing non-admin users from redirecting the poller to arbitrary URLs.
- HTTP requests to the monitoring URL use `HttpClient` with a bounded 10 s timeout (configurable, enforced by `MonitoringOptions`).

---

## 10. EF Core Migration

**Decision**: A single EF Core migration adds `MonitoringUrl` (nullable `TEXT`) and `MonitoringPollingIntervalSeconds` (nullable `INTEGER`) to the `Environments` table.

**Rationale**: The change is backward-compatible — existing rows get `NULL` for both columns and continue to work as before. No data backfill needed.

**Migration name**: `AddEnvironmentMonitoring`

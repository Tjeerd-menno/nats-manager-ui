# Implementation Plan: Live Environment Monitoring

**Branch**: `copilot/add-live-monitoring-feature` | **Date**: 2026-04-25 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/copilot/add-live-monitoring-feature/spec.md`

## Summary

Add live NATS server monitoring to the application. The backend polls each environment's NATS HTTP monitoring API (`/varz`, `/jsz`) at a configurable interval, stores snapshots in an in-memory ring buffer, and pushes updates to the frontend via ASP.NET Core SignalR. The frontend adds a Monitoring page per environment with Recharts time-series graphs for connections, message/byte rates, and JetStream stats — updated in real-time without polling from the browser.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core 10 SignalR (built-in, no new NuGet), `@microsoft/signalr` (new npm dep), `System.Net.Http.HttpClient` (built-in), Recharts 3.8.1 (existing)  
**Storage**: In-memory ring buffer only (no SQLite persistence for monitoring data). Two new nullable columns on existing `Environments` SQLite table via EF Core migration.  
**Testing**: xUnit + NSubstitute (backend), Vitest + React Testing Library (frontend)  
**Target Platform**: Desktop browsers; same Linux OCI container as main app  
**Project Type**: Feature addition to existing web application (SPA + Minimal API)  
**Performance Goals**: SignalR push latency ≤ 500 ms; monitoring page initial load ≤ 1 s; memory growth ≤ 2 MB/hour for 10 environments at 30 s polling  
**Constraints**: Monitoring data is ephemeral (no SQLite write path); polling interval 5–300 s; in-memory buffer capped at 120 snapshots per environment; NATS monitoring URL is per-environment opt-in  
**Scale/Scope**: Up to 10 environments with monitoring enabled; 120 snapshots × ~500 bytes × 10 environments ≈ 600 KB in-memory

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality (NON-NEGOTIABLE) — ✅ PASS

- New `Monitoring` module has a single, well-defined responsibility (metric polling + real-time push)
- All new code follows existing lint/format conventions (`dotnet format`, ESLint + Prettier)
- `NatsMonitoringHttpAdapter`, `MonitoringPoller`, `MonitoringHub`, `MonitoringMetricsStore` each ≤ 40 lines of logic
- NATS domain terminology used accurately (`streams`, `consumers`, `connections`)
- TypeScript strict mode enforced; no `any` types; explicit interface definitions for all hub payload types
- `@microsoft/signalr` dependency justified (required for SignalR client; no alternative in project)

### II. Testing Standards (NON-NEGOTIABLE) — ✅ PASS

- Unit tests for `NatsMonitoringHttpAdapter` (mocked HttpClient), `MonitoringMetricsStore` (ring buffer capping), `MonitoringPoller` (interval logic), and `MonitoringHub` (group subscription)
- Integration test verifying end-to-end polling cycle → SignalR push flow
- Frontend unit tests for `useMonitoringHub` hook and `MonitoringPage` component
- 80% coverage target for new code
- No direct NATS monitoring HTTP calls in tests — `IMonitoringAdapter` is mocked at boundary

### III. User Experience Consistency — ✅ PASS

- Monitoring page follows existing navigation pattern (left sidebar, environment context visible)
- Loading, empty (no monitoring URL), error, and connected states handled explicitly
- `DataFreshnessIndicator` reused to show last-updated timestamp
- Consistent layout with Dashboard page (cards + charts below)
- Environment context badge visible at all times (shared layout)

### IV. Performance Requirements — ✅ PASS

- In-memory ring buffer bounds memory (600 KB for 10 envs, ≤ 5 MB constitution limit)
- Recharts with max 120 data points per series renders well within 200 ms
- SignalR uses WebSocket transport (low overhead); no polling from browser
- `HttpClient` calls bounded by `HttpTimeoutSeconds` (10 s default, matches NATS 10 s timeout rule)
- No memory leak: hub connections cleaned up on disconnect; `useEffect` cleanup stops SignalR

## Project Structure

### Documentation (this feature)

```text
specs/copilot/add-live-monitoring-feature/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api-contracts.md # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── NatsManager.Domain/
│   └── Modules/
│       └── Environments/
│           └── Environment.cs                  # + MonitoringUrl, MonitoringPollingIntervalSeconds fields
│
├── NatsManager.Application/
│   └── Modules/
│       ├── Environments/
│       │   └── Commands/UpdateEnvironmentCommand.cs  # + new monitoring fields
│       └── Monitoring/                         # NEW bounded context
│           ├── Models/
│           │   └── MonitoringModels.cs          # MonitoringSnapshot, ServerMetrics, JetStreamMetrics, MonitoringStatus
│           └── Ports/
│               ├── IMonitoringAdapter.cs        # FetchSnapshotAsync(environment)
│               └── IMonitoringMetricsStore.cs   # AddSnapshot, GetHistory, GetLatest
│
├── NatsManager.Infrastructure/
│   ├── Migrations/
│   │   └── *_AddEnvironmentMonitoring.cs       # EF Core migration
│   ├── Monitoring/
│   │   └── MonitoringMetricsStore.cs           # In-memory ring buffer implementation
│   └── Nats/
│       └── NatsMonitoringHttpAdapter.cs        # HttpClient → /varz, /jsz, /healthz
│
└── NatsManager.Web/
    ├── appsettings.json                        # + "Monitoring" section
    ├── BackgroundServices/
    │   └── MonitoringPoller.cs                 # NEW: polls each env, stores snapshot, broadcasts
    ├── Endpoints/
    │   └── MonitoringEndpoints.cs              # GET history endpoint
    ├── Hubs/
    │   └── MonitoringHub.cs                    # NEW: SignalR hub
    └── Program.cs                              # + AddSignalR(), MapHub<MonitoringHub>(), DI wiring

src/NatsManager.Frontend/
└── src/
    └── features/
        └── monitoring/                         # NEW feature module
            ├── MonitoringPage.tsx
            ├── MonitoringPage.test.tsx
            ├── types.ts
            ├── hooks/
            │   ├── useMonitoringHub.ts         # SignalR connection lifecycle
            │   └── useMonitoringHub.test.ts
            └── components/
                ├── ServerMetricsChart.tsx      # Recharts LineChart: connections, msg/s, bytes/s
                ├── JetStreamMetricsCard.tsx    # Summary card + trend area chart
                └── MonitoringStatusBadge.tsx   # Connected / Reconnecting / Disconnected

tests/
├── NatsManager.Application.Tests/
│   └── Modules/Monitoring/                    # Unit tests for monitoring use cases / models
└── NatsManager.Infrastructure.Tests/
    └── Monitoring/                            # Unit tests for adapter + metrics store
```

**Structure Decision**: Feature additions fit entirely within the existing Clean Architecture layers. The new `Monitoring` module follows the same bounded-context pattern as `Dashboard`, `JetStream`, etc. No new .NET projects are added. The frontend adds a single new feature folder under `src/features/monitoring`, mirroring the backend module.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| New npm dependency (`@microsoft/signalr`) | SignalR JavaScript client is required to connect to the ASP.NET Core SignalR hub | No existing dependency provides SignalR WebSocket protocol support; axios/fetch cannot speak the SignalR protocol |

# Feature Specification: Live Environment Monitoring

**Feature Branch**: `copilot/add-live-monitoring-feature`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "I want to add live monitoring for the selected environment to the solution. The backend should poll the nats monitoring endpoints with a configurable polling interval. The Frontend should render graphs and show metric using websockets/SignalR."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — View Live Server Metrics (Priority: P1)

An operator selects an environment and navigates to a Monitoring page. The page connects via SignalR and starts receiving live metric snapshots as the backend polls the NATS monitoring HTTP API. Time-series graphs update in real-time showing connections, message rates (in/out per second), and byte rates.

**Why this priority**: Core deliverable — without live graphs this feature has no value. It is the visible proof-of-concept that the full backend-to-frontend pipeline works.

**Independent Test**: Can be fully tested by navigating to the Monitoring page for a connected environment and observing that graphs update automatically without page refresh.

**Acceptance Scenarios**:

1. **Given** an environment with a valid monitoring URL is selected, **When** the Monitoring page loads, **Then** the page connects to the SignalR hub and within one polling interval displays a line graph of connection counts over time.
2. **Given** the Monitoring page is active, **When** the backend completes a polling cycle, **Then** the SignalR hub pushes the new snapshot and the graph updates without a full page reload.
3. **Given** the monitoring URL is unreachable, **When** a polling cycle fails, **Then** the frontend displays a "Monitoring unavailable" alert while keeping the last known data visible.

---

### User Story 2 — View JetStream Monitoring Metrics (Priority: P2)

An operator wants to see live JetStream metrics: stream count, consumer count, total messages, and total bytes — plotted over time to detect trends.

**Why this priority**: JetStream health is the most operationally critical NATS capability; trend graphs catch degradation before it becomes an incident.

**Independent Test**: Can be tested independently by confirming that the JetStream metrics card/graph shows data sourced from `/jsz` and updates with each polling cycle.

**Acceptance Scenarios**:

1. **Given** JetStream is enabled in the selected environment, **When** the Monitoring page loads, **Then** a JetStream section shows current stream count, consumer count, total messages, and total bytes.
2. **Given** a new stream is created in NATS, **When** the next polling cycle completes, **Then** the stream count in the monitoring view increments without manual refresh.

---

### User Story 3 — Configure Polling Interval (Priority: P3)

An operator or administrator can configure the polling interval for a specific environment (e.g., 10 s, 30 s, 60 s). The default is 30 seconds. Changes take effect without restarting the application.

**Why this priority**: Operational teams have different needs; high-frequency polling during incidents, low-frequency polling for stable environments.

**Independent Test**: Can be tested by changing the polling interval setting for an environment and observing that graph data points appear at the new cadence.

**Acceptance Scenarios**:

1. **Given** an environment has a default polling interval, **When** an administrator updates it to 10 seconds, **Then** subsequent polling cycles run at 10-second intervals.
2. **Given** a polling interval is set to 60 seconds, **When** the monitoring page is open, **Then** the graph shows one new data point approximately every 60 seconds.

---

### User Story 4 — Configure Monitoring URL per Environment (Priority: P4)

An operator can set an optional monitoring URL per environment (e.g., `http://nats-server:8222`) in the environment settings. If not set, monitoring is disabled for that environment.

**Why this priority**: NATS monitoring HTTP API runs on a separate port from the NATS connection. Environments that don't expose it should gracefully disable this feature.

**Independent Test**: Can be tested by adding a monitoring URL to an existing environment and confirming that the Monitoring page becomes active for that environment.

**Acceptance Scenarios**:

1. **Given** an environment has no monitoring URL configured, **When** a user navigates to its Monitoring page, **Then** an informational message explains that monitoring is not configured for this environment.
2. **Given** an environment has a monitoring URL configured, **When** the URL is reachable, **Then** polling starts automatically when the application starts.

---

### Edge Cases

- What happens when the NATS monitoring endpoint returns an unexpected JSON structure? → The adapter should log a warning and skip that polling cycle; no crash.
- How does the system handle a SignalR client that reconnects mid-session? → Reconnection restores group membership; the client re-receives the latest cached snapshot.
- What happens if 10 environments all have monitoring enabled simultaneously? → Each has its own polling timer; memory is bounded by the per-environment ring buffer (max 120 snapshots × 10 environments = bounded constant).
- What happens when the frontend page is closed while connected to the SignalR hub? → The SignalR connection is cleaned up automatically (browser unload / React `useEffect` cleanup); the server removes the client from the group.
- What if the monitoring URL uses HTTPS with a self-signed certificate? → The HTTP client should be configurable to bypass certificate validation per environment (admin-only setting, off by default).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-MON-001**: Each `Environment` entity MUST have an optional `MonitoringUrl` field (nullable string) and an optional `MonitoringPollingIntervalSeconds` (nullable int).
- **FR-MON-002**: The backend MUST provide a `BackgroundService` that polls each enabled environment's monitoring URL at its configured interval (or the global default of 30 s).
- **FR-MON-003**: The backend MUST fetch `/varz` from the NATS monitoring HTTP API to obtain server-level metrics (connections, in/out messages, in/out bytes, uptime, version).
- **FR-MON-004**: The backend MUST fetch `/jsz` from the NATS monitoring HTTP API to obtain JetStream-level metrics (stream count, consumer count, total messages, total bytes).
- **FR-MON-005**: The backend MUST maintain an in-memory circular buffer of up to 120 metric snapshots per environment (approx. 1 hour at 30 s default interval).
- **FR-MON-006**: The backend MUST expose a SignalR hub at `/hubs/monitoring` that allows clients to subscribe to a specific environment's metric stream.
- **FR-MON-007**: The SignalR hub MUST push a `MonitoringSnapshot` message to subscribed clients whenever a new polling cycle completes.
- **FR-MON-008**: The backend MUST expose a REST endpoint `GET /api/environments/{envId}/monitoring/metrics/history` that returns the current in-memory snapshot history (for initial page load before real-time updates begin).
- **FR-MON-009**: The global default polling interval MUST be configurable via `appsettings.json` under `Monitoring:DefaultPollingIntervalSeconds`.
- **FR-MON-010**: Administrators MUST be able to update `MonitoringUrl` and `MonitoringPollingIntervalSeconds` per environment through the existing environment update endpoint (or a dedicated sub-resource).
- **FR-MON-011**: The frontend MUST display a Monitoring page accessible from the environment navigation for environments that have a monitoring URL configured.
- **FR-MON-012**: The Monitoring page MUST display at minimum: a server metrics time-series graph (connections, msg/s in, msg/s out, bytes/s in, bytes/s out) and a JetStream summary card with trend indicators.
- **FR-MON-013**: The frontend MUST gracefully handle SignalR disconnections with automatic reconnection and a visible "reconnecting…" status indicator.
- **FR-MON-014**: The frontend MUST show the "last updated" timestamp alongside all metric displays.
- **FR-MON-015**: Monitoring data MUST NOT be persisted to SQLite — it is purely in-memory (ring buffer) and real-time.

### Key Entities

- **MonitoringSnapshot**: A point-in-time metric capture per environment. Contains timestamp, server vars (connections, message rates, byte rates, uptime, version), and JetStream stats (stream count, consumer count, total messages, total bytes).
- **MonitoringMetricsStore**: An in-memory store keyed by environment ID, holding a circular buffer of `MonitoringSnapshot` records.
- **MonitoringConfiguration**: The polling interval and monitoring URL associated with each environment (extends the `Environment` entity).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-MON-001**: Monitoring page updates with live data within the configured polling interval (±2 s tolerance) after page load.
- **SC-MON-002**: Memory growth from 10 environments at 30 s polling is ≤ 2 MB per hour (well within the 5 MB/hour constitution limit).
- **SC-MON-003**: The SignalR push latency from backend poll completion to frontend graph update is ≤ 500 ms on localhost.
- **SC-MON-004**: The Monitoring page's initial load (history fetch + SignalR connect) completes within 1 s on a local network.
- **SC-MON-005**: All 80%-coverage unit tests pass for the new `Monitoring` module in both backend and frontend.

## Assumptions

- NATS monitoring HTTP API is accessible at a URL separate from the NATS connection URL (e.g., `http://nats-host:8222`).
- Not all environments will have monitoring enabled; those without `MonitoringUrl` set are silently excluded from polling.
- Metric history is ephemeral — application restart clears the in-memory buffer (no persistence requirement).
- The polling interval can be changed at runtime for individual environments but takes effect only on the next polling cycle start.
- The `@microsoft/signalr` npm package will be added to the frontend as a new dependency (currently absent).
- ASP.NET Core SignalR is used (built into .NET SDK; no additional NuGet package required for the server side).
- Rate-derived metrics (msg/s, bytes/s) are computed by the backend from consecutive snapshots rather than directly from NATS.

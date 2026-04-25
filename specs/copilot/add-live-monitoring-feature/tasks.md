# Tasks: Live Environment Monitoring

**Input**: Design documents from `/specs/copilot/add-live-monitoring-feature/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Included — spec requires xUnit + NSubstitute (backend) and Vitest + React Testing Library (frontend) with 80% coverage target for new code.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US4)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install new dependency and add `MonitoringOptions` configuration class — prerequisite for all subsequent phases.

- [ ] T001 Install `@microsoft/signalr` npm package in `src/NatsManager.Frontend/`: `npm install @microsoft/signalr` (adds `@microsoft/signalr@8.x` to `package.json` `dependencies`)
- [ ] T002 [P] Add `MonitoringOptions` configuration class in `src/NatsManager.Web/Configuration/MonitoringOptions.cs` with properties `DefaultPollingIntervalSeconds` (int, default 30), `MaxSnapshotsPerEnvironment` (int, default 120), `HttpTimeoutSeconds` (int, default 10); add `"Monitoring"` section to `src/NatsManager.Web/appsettings.json` and `src/NatsManager.Web/appsettings.Development.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain extensions, Application ports/models, Infrastructure implementations, EF Core migration, and DI registration that ALL user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T003 Extend `Environment` domain entity in `src/NatsManager.Domain/Modules/Environments/Environment.cs`: add `MonitoringUrl` (string?, private set) and `MonitoringPollingIntervalSeconds` (int?, private set); add `UpdateMonitoringSettings(string? monitoringUrl, int? pollingIntervalSeconds)` method with validation (URL must be http/https if provided, interval 5–300 if provided)
- [ ] T004 [P] Create Application-layer monitoring models in `src/NatsManager.Application/Modules/Monitoring/Models/MonitoringModels.cs`: `MonitoringSnapshot` record (EnvironmentId, Timestamp, Server: ServerMetrics, JetStream: JetStreamMetrics?, Status: MonitoringStatus), `ServerMetrics` record (all fields from data-model.md §2 including derived per-second rates), `JetStreamMetrics` record (StreamCount, ConsumerCount, TotalMessages, TotalBytes), `MonitoringStatus` enum (Ok, Degraded, Unavailable), `MonitoringHistoryResult` record (EnvironmentId, Snapshots: IReadOnlyList\<MonitoringSnapshot\>)
- [ ] T005 [P] Create Application-layer ports in `src/NatsManager.Application/Modules/Monitoring/Ports/`: `IMonitoringAdapter.cs` (`Task<MonitoringSnapshot?> FetchSnapshotAsync(Domain.Modules.Environments.Environment environment, MonitoringSnapshot? previous, CancellationToken ct)`) and `IMonitoringMetricsStore.cs` (`void AddSnapshot(MonitoringSnapshot snapshot)`, `IReadOnlyList<MonitoringSnapshot> GetHistory(Guid environmentId)`, `MonitoringSnapshot? GetLatest(Guid environmentId)`)
- [ ] T006 Create EF Core migration `AddEnvironmentMonitoring` in `src/NatsManager.Infrastructure/Persistence/Migrations/`: adds nullable `MonitoringUrl TEXT` and `MonitoringPollingIntervalSeconds INTEGER` columns to `Environments` table; update `AppDbContext` entity configuration for `Environment` to map both new properties
- [ ] T007 [P] Implement `NatsMonitoringHttpAdapter` in `src/NatsManager.Infrastructure/Nats/NatsMonitoringHttpAdapter.cs`: inject `IHttpClientFactory`; implement `IMonitoringAdapter`; `FetchSnapshotAsync` — GET `{monitoringUrl}/varz` for `NatsVarzResponse`, optionally GET `{monitoringUrl}/jsz` for `NatsJszResponse` (skip on 404 = JetStream disabled), derive per-second rates from previous snapshot cumulative counters, return `MonitoringSnapshot`; internal `NatsVarzResponse` and `NatsJszResponse` DTO records with `[JsonPropertyName]` attributes matching NATS field names (data-model.md §5); handle `HttpRequestException` / `TaskCanceledException` → return snapshot with `Status = Unavailable`
- [ ] T008 [P] Implement `MonitoringMetricsStore` in `src/NatsManager.Infrastructure/Monitoring/MonitoringMetricsStore.cs`: implement `IMonitoringMetricsStore`; internal `EnvironmentMetricsBuffer` with `Queue<MonitoringSnapshot>` capped at `MaxSnapshotsPerEnvironment` and a `lock` object for thread safety; `ConcurrentDictionary<Guid, EnvironmentMetricsBuffer>` keyed by environment ID; inject `IOptions<MonitoringOptions>` for capacity
- [ ] T009 Register services in `src/NatsManager.Web/Program.cs`: `builder.Services.Configure<MonitoringOptions>(...)`, `builder.Services.AddHttpClient()`, `builder.Services.AddSignalR()`, register `IMonitoringAdapter` → `NatsMonitoringHttpAdapter` (singleton), register `IMonitoringMetricsStore` → `MonitoringMetricsStore` (singleton)

**Checkpoint**: Foundation ready — models, ports, infrastructure implementations, migration, and DI wiring complete

---

## Phase 3: User Story 1 — View Live Server Metrics (Priority: P1) 🎯 MVP

**Goal**: Operators can open the Monitoring page for an environment that has a `MonitoringUrl` set and see live server-metric graphs (connections, message rates, byte rates) auto-updating via SignalR.

**Independent Test**: Navigate to the Monitoring page for a connected environment with monitoring configured and confirm that connection/message-rate graphs appear and update each polling cycle without page refresh.

### Tests for User Story 1

> **Write these tests FIRST — ensure they FAIL before implementation**

- [ ] T010 [P] [US1] Unit tests for `NatsMonitoringHttpAdapter` in `tests/NatsManager.Infrastructure.Tests/Monitoring/NatsMonitoringHttpAdapterTests.cs`: mock `HttpMessageHandler` returning sample `/varz` JSON → verify `ServerMetrics` fields; verify rate derivation from two consecutive snapshots; verify `Status = Unavailable` on `HttpRequestException`
- [ ] T011 [P] [US1] Unit tests for `MonitoringMetricsStore` in `tests/NatsManager.Infrastructure.Tests/Monitoring/MonitoringMetricsStoreTests.cs`: verify `AddSnapshot` stores snapshot; verify buffer caps at `MaxSnapshotsPerEnvironment` (oldest evicted); verify `GetHistory` returns snapshots in insertion order; verify `GetLatest` returns most recent
- [ ] T012 [P] [US1] Unit tests for `MonitoringPoller` in `tests/NatsManager.Web.Tests/BackgroundServices/MonitoringPollerTests.cs`: mock `IEnvironmentRepository`, `IMonitoringAdapter`, `IMonitoringMetricsStore`, `IHubContext<MonitoringHub>`; verify only environments with `MonitoringUrl != null` are polled; verify `AddSnapshot` and hub broadcast are called on successful poll; verify polling continues after a single environment poll failure
- [ ] T013 [P] [US1] Contract tests for monitoring history endpoint in `tests/NatsManager.Web.Tests/Endpoints/MonitoringEndpointTests.cs`: `GET /api/environments/{envId}/monitoring/metrics/history` returns `200` with history list; returns `404` for unknown environment; returns `400` when `MonitoringUrl` is null
- [ ] T014 [P] [US1] Frontend hook tests in `src/NatsManager.Frontend/src/features/monitoring/hooks/useMonitoringHub.test.ts`: mock `@microsoft/signalr` `HubConnectionBuilder`; verify initial history is fetched on mount; verify `SubscribeToEnvironment` is invoked; verify `snapshots` state appends on `ReceiveMonitoringSnapshot` messages; verify connection is stopped on unmount
- [ ] T015 [P] [US1] Frontend component tests in `src/NatsManager.Frontend/src/features/monitoring/MonitoringPage.test.tsx`: renders `LoadingState` while connecting; renders `ServerMetricsChart` when snapshots are available; renders error alert when `connectionStatus === 'disconnected'`; renders empty-state when environment has no monitoring URL

### Implementation for User Story 1

- [ ] T016 [US1] Implement `MonitoringHub` in `src/NatsManager.Web/Hubs/MonitoringHub.cs`: `[Authorize]` attribute; `Task SubscribeToEnvironment(string environmentId)` → `Groups.AddToGroupAsync(Context.ConnectionId, $"env-{environmentId}")`; `Task UnsubscribeFromEnvironment(string environmentId)` → `Groups.RemoveFromGroupAsync(...)`
- [ ] T017 [US1] Implement `MonitoringPoller` background service in `src/NatsManager.Web/BackgroundServices/MonitoringPoller.cs`: inject `IServiceScopeFactory`, `IMonitoringAdapter`, `IMonitoringMetricsStore`, `IHubContext<MonitoringHub>`, `IOptions<MonitoringOptions>`, `ILogger<MonitoringPoller>`; per-environment polling loop: resolve interval (env override ?? global default); call `IMonitoringAdapter.FetchSnapshotAsync`; `IMonitoringMetricsStore.AddSnapshot`; `hubContext.Clients.Group($"env-{envId}").SendAsync("ReceiveMonitoringSnapshot", snapshot)`; track previous snapshot per environment for rate derivation; register as `AddHostedService<MonitoringPoller>()` in `Program.cs`
- [ ] T018 [US1] Implement monitoring history endpoint in `src/NatsManager.Web/Endpoints/MonitoringEndpoints.cs`: `GET /api/environments/{envId:guid}/monitoring/metrics/history` → validates environment exists + has monitoring URL (404/400 as per contracts §1.1) → returns `IMonitoringMetricsStore.GetHistory(envId)` wrapped in `MonitoringHistoryResult`; map hub route `app.MapHub<MonitoringHub>("/hubs/monitoring")` in `Program.cs`; call `app.MapMonitoringEndpoints()` in `Program.cs`
- [ ] T019 [P] [US1] Create frontend types in `src/NatsManager.Frontend/src/features/monitoring/types.ts`: `MonitoringSnapshot`, `ServerMetrics`, `JetStreamMetrics`, `MonitoringConnectionStatus` interfaces as specified in data-model.md §8, plus `MonitoringHistoryResult` matching the history endpoint response shape
- [ ] T020 [US1] Implement `useMonitoringHub` hook in `src/NatsManager.Frontend/src/features/monitoring/hooks/useMonitoringHub.ts`: on mount fetch history from `GET /api/environments/{envId}/monitoring/metrics/history`, seed `snapshots` state; build `HubConnection` with `.withUrl('/hubs/monitoring', { withCredentials: true }).withAutomaticReconnect()`; handle `onreconnecting`/`onreconnected`/`onclose` to update `connectionStatus`; on `ReceiveMonitoringSnapshot` prepend snapshot and trim to 120; on unmount invoke `UnsubscribeFromEnvironment` and stop connection; return `{ snapshots, latestSnapshot, connectionStatus, error }`
- [ ] T021 [P] [US1] Implement `MonitoringStatusBadge` component in `src/NatsManager.Frontend/src/features/monitoring/components/MonitoringStatusBadge.tsx`: Mantine `Badge` with colour: green for `connected`, yellow for `reconnecting`, red for `disconnected`, grey for `connecting`; includes pulsing dot for `connected` state
- [ ] T022 [P] [US1] Implement `ServerMetricsChart` component in `src/NatsManager.Frontend/src/features/monitoring/components/ServerMetricsChart.tsx`: Recharts `<LineChart>` with two panels — (1) connections over time as a `<Line>`, (2) message rates (`inMsgsPerSec`, `outMsgsPerSec`) as two `<Line>` series; `<XAxis>` formatted as local time string from `timestamp`; `<YAxis>` auto-domain; `<Tooltip>` with formatted values; `<Legend>`; accepts `snapshots: MonitoringSnapshot[]` prop; renders `EmptyState` when `snapshots.length === 0`
- [ ] T023 [US1] Implement `MonitoringPage` in `src/NatsManager.Frontend/src/features/monitoring/MonitoringPage.tsx`: uses `useEnvironmentContext()` for `selectedEnvironmentId`; calls `useMonitoringHub(selectedEnvironmentId)`; renders `EmptyState` when no environment selected or monitoring URL not configured (detected via `400` from history endpoint); renders `LoadingState` while `connectionStatus === 'connecting'`; renders `MonitoringStatusBadge` + `DataFreshnessIndicator` + `ServerMetricsChart` in a `Stack`; renders error `Alert` on `connectionStatus === 'disconnected'`
- [ ] T024 [US1] Add Monitoring route and nav link in `src/NatsManager.Frontend/src/App.tsx` (lazy import `MonitoringPage`) and in the sidebar `AppLayout` component: add "Monitoring" nav item with activity icon, shown for all environments (disabled/greyed when monitoring not configured)

**Checkpoint**: User Story 1 complete — live server metric graphs visible and auto-updating via SignalR for any environment with a monitoring URL configured

---

## Phase 4: User Story 2 — View JetStream Monitoring Metrics (Priority: P2)

**Goal**: JetStream metrics (stream count, consumer count, total messages, total bytes) from `/jsz` are visible as a live card with trend graph on the Monitoring page alongside the server metrics.

**Independent Test**: Navigate to the Monitoring page for an environment with JetStream enabled and confirm that the JetStream card shows counts and the trend area chart updates each polling cycle.

### Tests for User Story 2

- [ ] T025 [P] [US2] Unit tests for JetStream metrics path in `tests/NatsManager.Infrastructure.Tests/Monitoring/NatsMonitoringHttpAdapterTests.cs` (extend T010 test file): mock `/jsz` returning `NatsJszResponse` → verify `JetStreamMetrics` populated on snapshot; mock `/jsz` returning 404 → verify `JetStream` is null on snapshot; mock `/jsz` returning malformed JSON → verify `Status = Degraded` and `JetStream` is null
- [ ] T026 [P] [US2] Frontend component tests in `src/NatsManager.Frontend/src/features/monitoring/components/JetStreamMetricsCard.test.tsx`: renders stream/consumer/message/bytes counts from `latestSnapshot.jetStream`; renders `null` state message when `latestSnapshot.jetStream` is null; renders area chart with trend from `snapshots`

### Implementation for User Story 2

- [ ] T027 [US2] Implement `JetStreamMetricsCard` component in `src/NatsManager.Frontend/src/features/monitoring/components/JetStreamMetricsCard.tsx`: Mantine `Card` with `SimpleGrid` of 4 stat cells (stream count, consumer count, total messages, total bytes with formatted large numbers); below the stats, a Recharts `<AreaChart>` plotting `totalMessages` over time using the `snapshots` array; renders a Mantine `Alert` with info icon when `jetStream === null` (JetStream not enabled)
- [ ] T028 [US2] Add `JetStreamMetricsCard` to `MonitoringPage` in `src/NatsManager.Frontend/src/features/monitoring/MonitoringPage.tsx`: render `JetStreamMetricsCard` below `ServerMetricsChart`, passing `snapshots` and `latestSnapshot`; wrap in a `Stack` with a "JetStream" section title

**Checkpoint**: User Stories 1 and 2 complete — server metrics graphs AND JetStream trend card both visible on the Monitoring page

---

## Phase 5: User Story 3 — Configure Polling Interval (Priority: P3)

**Goal**: Administrators can set a per-environment polling interval (5–300 s) that overrides the global default; the `MonitoringPoller` picks up the new value without restart.

**Independent Test**: Update a monitored environment's polling interval to 10 s and observe that new graph data points appear approximately every 10 seconds.

### Tests for User Story 3

- [ ] T029 [P] [US3] Unit tests for polling interval logic in `tests/NatsManager.Web.Tests/BackgroundServices/MonitoringPollerTests.cs` (extend T012 test file): mock environment with `MonitoringPollingIntervalSeconds = 10` → verify adapter is called at 10 s cadence; mock environment with `MonitoringPollingIntervalSeconds = null` → verify global default interval (30 s) is used
- [ ] T030 [P] [US3] Unit tests for `UpdateEnvironmentCommand` validator in `tests/NatsManager.Application.Tests/Modules/Environments/UpdateEnvironmentCommandTests.cs` (extend existing file): verify `MonitoringPollingIntervalSeconds = 4` fails validation; verify `MonitoringPollingIntervalSeconds = 5` passes; verify `MonitoringPollingIntervalSeconds = 300` passes; verify `MonitoringPollingIntervalSeconds = 301` fails

### Implementation for User Story 3

- [ ] T031 [US3] Extend `UpdateEnvironmentCommand` in `src/NatsManager.Application/Modules/Environments/Commands/UpdateEnvironmentCommand.cs`: add `MonitoringPollingIntervalSeconds` (int?) field; extend FluentValidation validator with rule: `When(x => x.MonitoringPollingIntervalSeconds.HasValue, () => RuleFor(x => x.MonitoringPollingIntervalSeconds!.Value).InclusiveBetween(5, 300))`; command handler calls `environment.UpdateMonitoringSettings(monitoringUrl, pollingIntervalSeconds)`
- [ ] T032 [US3] Update `MonitoringPoller` in `src/NatsManager.Web/BackgroundServices/MonitoringPoller.cs` to re-read `environment.MonitoringPollingIntervalSeconds` at the start of each per-environment cycle (scoped repository call inside the loop) so runtime interval changes are effective from the next cycle without restart

**Checkpoint**: Polling interval is configurable per environment; changes are effective within one polling cycle

---

## Phase 6: User Story 4 — Configure Monitoring URL per Environment (Priority: P4)

**Goal**: Administrators can set (or clear) `MonitoringUrl` on an environment via the environment edit form. Environments without a URL show an informational empty state on the Monitoring page.

**Independent Test**: Edit an environment to add a monitoring URL, navigate to the Monitoring page, and confirm it transitions from empty-state to displaying live data.

### Tests for User Story 4

- [ ] T033 [P] [US4] Unit tests for `Environment.UpdateMonitoringSettings` in `tests/NatsManager.Domain.Tests/Modules/Environments/EnvironmentTests.cs` (extend existing file): setting valid `http://host:8222` → stored; setting `https://host:8443` → stored; setting null → clears field; setting `ftp://invalid` → throws `ArgumentException`; setting URL > 500 chars → throws `ArgumentException`
- [ ] T034 [P] [US4] Unit tests for `UpdateEnvironmentCommand` validator (monitoring URL) in `tests/NatsManager.Application.Tests/Modules/Environments/UpdateEnvironmentCommandTests.cs` (extend T030 file): valid `http://host:8222` passes; `https://` passes; `ftp://` fails; empty string fails; length > 500 fails; null passes (monitoring disabled)
- [ ] T035 [P] [US4] Contract test for update environment endpoint with monitoring fields in `tests/NatsManager.Web.Tests/Endpoints/EnvironmentEndpointTests.cs` (extend existing file): `PUT` with valid `monitoringUrl` and `monitoringPollingIntervalSeconds` returns `200`; `PUT` with invalid URL returns `422` with field error; `PUT` with `monitoringUrl: null` clears monitoring (environment no longer polled)
- [ ] T036 [P] [US4] Frontend form tests in `src/NatsManager.Frontend/src/features/environments/EnvironmentForm.test.tsx` (extend existing file): renders "Monitoring URL" field; renders "Polling Interval" number input; submits with valid URL updates the field; displays validation error when non-HTTP URL entered

### Implementation for User Story 4

- [ ] T037 [US4] Extend `UpdateEnvironmentCommand` validator in `src/NatsManager.Application/Modules/Environments/Commands/UpdateEnvironmentCommand.cs` (building on T031): add `MonitoringUrl` (string?) field; validator rule: `When(x => !string.IsNullOrWhiteSpace(x.MonitoringUrl), () => RuleFor(x => x.MonitoringUrl!).Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https")).WithMessage("MonitoringUrl must be a valid http:// or https:// URL").MaximumLength(500))`
- [ ] T038 [P] [US4] Update environment edit form in `src/NatsManager.Frontend/src/features/environments/components/EnvironmentForm.tsx`: add Mantine `TextInput` for "Monitoring URL" (placeholder `http://nats-server:8222`, optional); add Mantine `NumberInput` for "Polling Interval (seconds)" (min 5, max 300, placeholder "Default (30)", optional); wire both fields to the existing form submission hook `useUpdateEnvironment`
- [ ] T039 [P] [US4] Update `useUpdateEnvironment` hook in `src/NatsManager.Frontend/src/features/environments/hooks/useUpdateEnvironment.ts` to include `monitoringUrl: string | null` and `monitoringPollingIntervalSeconds: number | null` in the request payload sent to `PUT /api/environments/{envId}`
- [ ] T040 [US4] Update `MonitoringPage` empty-state handling in `src/NatsManager.Frontend/src/features/monitoring/MonitoringPage.tsx`: when history endpoint returns `400` (no monitoring URL), render a Mantine `Alert` with `info` colour explaining "Monitoring is not configured for this environment. Edit the environment to add a Monitoring URL."

**Checkpoint**: All user stories complete — monitoring URL and polling interval can be configured per environment from the UI

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final wiring, byte-rate chart, logged structured events, and validation of quickstart.md

- [ ] T041 [P] Add byte-rate panel to `ServerMetricsChart` in `src/NatsManager.Frontend/src/features/monitoring/components/ServerMetricsChart.tsx`: add a second `<LineChart>` (or second `<YAxis>`) for `inBytesPerSec` and `outBytesPerSec` with byte-formatted tooltip (KB/s, MB/s)
- [ ] T042 [P] Add structured log messages to `MonitoringPoller` in `src/NatsManager.Web/BackgroundServices/MonitoringPoller.cs`: `[LoggerMessage]` partial methods for `PollerStarted`, `PollerStopped`, `PollCycleComplete(envName, snapshotCount)`, `PollCycleFailed(envName, error)`, `MonitoringUrlNotConfigured(envName)` — consistent with `EnvironmentHealthPoller` style
- [ ] T043 [P] Add structured log messages to `NatsMonitoringHttpAdapter` in `src/NatsManager.Infrastructure/Nats/NatsMonitoringHttpAdapter.cs`: `[LoggerMessage]` partial methods for `FetchSuccess(url, latencyMs)`, `FetchFailed(url, error)`, `JetStreamUnavailable(url)`
- [ ] T044 [P] Register `IHttpClientFactory`-managed named client `"NatsMonitoring"` in `Program.cs` with `DefaultRequestVersion = HttpVersion.Version11`, `Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)`; update `NatsMonitoringHttpAdapter` to resolve named client
- [ ] T045 Run `quickstart.md` validation: apply migration (`dotnet ef database update`), configure monitoring URL on an environment, start application, verify Monitoring page displays graphs and SignalR connection badge shows "Connected"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — core pipeline
- **User Story 2 (Phase 4)**: Depends on Phase 2; integrates with Phase 3 MonitoringPage but adds only a new component
- **User Story 3 (Phase 5)**: Depends on Phase 2 (MonitoringPoller interval logic) and Phase 3 (MonitoringPoller exists)
- **User Story 4 (Phase 6)**: Depends on Phase 2 (domain entity, validator) and Phase 3 (MonitoringPage empty-state); integrates with existing environment edit form
- **Polish (Phase 7)**: Depends on all story phases being complete

### User Story Dependencies

- **US1 (P1)**: Start after Phase 2 — no dependency on US2/US3/US4
- **US2 (P2)**: Start after Phase 2 — adds JetStreamMetricsCard; no dependency on US1 completion (components independent)
- **US3 (P3)**: Start after Phase 2 — extends MonitoringPoller (created in US1 Phase 3) and UpdateEnvironmentCommand; depends on T017 (MonitoringPoller) existing
- **US4 (P4)**: Start after Phase 2 — extends domain + validator + environment form; depends on T003 (Environment entity) and T023 (MonitoringPage empty-state) being complete

### Parallel Opportunities

- All `[P]`-marked tasks within a phase can be executed concurrently
- Phase 3 (US1) and Phase 4 (US2) can run in parallel after Phase 2 (backend work is independent, frontend components are independent)
- Phase 5 (US3) backend tasks (T031, T032) can start as soon as T017 (MonitoringPoller) is merged
- Phase 6 (US4) backend tasks (T037, T038, T039) can start as soon as T003 is merged

### Within Each Phase

- Tests (T010–T015, T025–T026, T029–T030, T033–T036) MUST be written and FAIL before implementation tasks in same phase
- Models/ports (T004, T005) before implementations (T007, T008)
- Infrastructure implementations (T007, T008) before background services (T017)
- Background service (T017) before endpoint (T018) — hub must be registered before endpoint maps it

---

## Parallel Example: Phase 3 (User Story 1)

```bash
# Write all tests for US1 together (they don't share files):
Task: "Unit tests for NatsMonitoringHttpAdapter"         → tests/NatsManager.Infrastructure.Tests/
Task: "Unit tests for MonitoringMetricsStore"           → tests/NatsManager.Infrastructure.Tests/
Task: "Unit tests for MonitoringPoller"                 → tests/NatsManager.Web.Tests/
Task: "Contract tests for monitoring history endpoint"  → tests/NatsManager.Web.Tests/
Task: "Frontend hook tests (useMonitoringHub)"          → src/NatsManager.Frontend/src/features/monitoring/
Task: "Frontend component tests (MonitoringPage)"       → src/NatsManager.Frontend/src/features/monitoring/

# After tests fail, implement in parallel where possible:
Task: "Implement MonitoringHub"                         → src/NatsManager.Web/Hubs/
Task: "Implement ServerMetricsChart"                    → src/NatsManager.Frontend/ (no backend dependency)
Task: "Implement MonitoringStatusBadge"                 → src/NatsManager.Frontend/ (no backend dependency)
Task: "Create frontend types"                           → src/NatsManager.Frontend/ (no backend dependency)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001, T002)
2. Complete Phase 2: Foundational (T003–T009) — CRITICAL
3. Complete Phase 3: User Story 1 (T010–T024)
4. **STOP and VALIDATE**: Open Monitoring page for a monitored environment, confirm real-time graph updates
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → live server metrics visible → Demo (MVP!)
3. US2 → JetStream trend card added → Demo
4. US3 → per-environment polling interval configurable → Demo
5. US4 → monitoring URL editable in environment form → Demo
6. Polish → byte-rate chart, logging, HttpClient timeout wired

### Parallel Team Strategy

With two developers after Phase 2 is complete:

- **Developer A**: Phase 3 (US1 backend: T016, T017, T018) + Phase 5 (US3 backend: T031, T032)
- **Developer B**: Phase 3 (US1 frontend: T019–T024) + Phase 4 (US2 frontend: T027, T028) + Phase 6 (US4 frontend: T038, T039, T040)

---

## Notes

- `[P]` tasks = different files, no shared dependencies within same phase
- Each user story is independently demonstrable after its phase is complete
- Rate fields (`inMsgsPerSec`, etc.) are `0.0` in the first snapshot per environment (no previous to diff against) — tests must account for this
- The `MonitoringPoller` must track the last snapshot per environment in-memory (not via `IMonitoringMetricsStore.GetLatest`) to avoid a lock round-trip on every poll cycle
- Do not add the `"Monitoring"` nav item to environments that have `monitoringUrl == null` — the page is accessible but shows the empty-state; the nav item is always visible for clarity
- Commit after each task or logical group; verify `dotnet test` and `npm test` pass before each commit

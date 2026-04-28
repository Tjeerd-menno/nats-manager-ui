# Tasks: Cluster Observability

**Input**: Design documents from `/specs/copilot/cluster-observability/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Included — repository convention requires xUnit v3 + NSubstitute (backend) and Vitest + React Testing Library (frontend) with contract tests at the API boundary.

**Organization**: Tasks grouped by user story for independent implementation and testing.

**Overlap / main-state note**: `origin/main` does not yet contain this feature folder, does not contain `@xyflow/react`/`reactflow`, and only has generic relationship/topology references in existing root guidance. All tasks below **extend** the existing `Monitoring` bounded context (`src/NatsManager.Application/Modules/Monitoring/`, `src/NatsManager.Infrastructure/Nats/` monitoring adapters, `src/NatsManager.Web/Endpoints/MonitoringEndpoints.cs`, `src/NatsManager.Frontend/src/features/monitoring/`) rather than duplicating Monitoring, resource, search, or alerts modules.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the new frontend dependency and create the empty module folders this feature will populate. No source code changes beyond folder creation and `package.json`/lockfile updates.

- [ ] T001 [P] Pin React Flow as a new frontend dependency in `src/NatsManager.Frontend/package.json` under `dependencies`: add `"@xyflow/react": "12.x"` (use a single resolved minor/patch version, e.g. `12.3.6`); run `npm install` to update `src/NatsManager.Frontend/package-lock.json`. **This task MUST complete before any task that imports from `@xyflow/react`.**
- [ ] T002 [P] Create backend folder skeleton under existing `Monitoring` module: `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/`, `src/NatsManager.Application/Modules/Monitoring/Ports/ClusterObservability/`, `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/`, `src/NatsManager.Infrastructure/Monitoring/ClusterObservability/`, `src/NatsManager.Infrastructure/Nats/ClusterObservability/` (placeholders only, no .cs files yet)
- [ ] T003 [P] Create frontend folder skeleton: `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/`, `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/hooks/`, `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/` (within the existing `monitoring` feature folder; do not create a sibling top-level feature)
- [ ] T004 [P] Create test folder skeleton: `tests/NatsManager.Application.Tests/Modules/Monitoring/ClusterObservability/`, `tests/NatsManager.Infrastructure.Tests/Monitoring/ClusterObservability/`, `tests/NatsManager.Infrastructure.Tests/Nats/ClusterObservability/`, `tests/NatsManager.Web.Tests/Monitoring/ClusterObservability/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared models, enums, ports, projection helpers, and HTTP monitoring client wiring used by all three user stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 [US-shared] Implement cluster observability enums in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/Enums.cs`: `ClusterStatus`, `ServerStatus`, `RelationshipStatus`, `ObservationFreshness`, `MetricState`, `TopologyRelationshipType`, `RelationshipDirection`, `MonitoringEndpoint` per data-model.md §6
- [ ] T006 [P] Implement `ServerObservation` record in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/ServerObservation.cs` per data-model.md §2 (nullable metric fields, `MetricStates` collection, `LastObservedAt`)
- [ ] T007 [P] Implement `TopologyRelationship` record in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/TopologyRelationship.cs` per data-model.md §3 (`SafeLabel`, `SourceEndpoint`, `Direction`, `Status`, `Freshness`)
- [ ] T008 [P] Implement `ClusterWarning` record in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/ClusterWarning.cs` (`Code`, `Severity`, `Message`, optional `ServerId`) referenced by overview response in api-contracts.md §1.1
- [ ] T009 Implement `ClusterObservation` record in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/ClusterObservation.cs` per data-model.md §1, referencing T005–T008 (composes `ServerObservation[]`, `TopologyRelationship[]`, `ClusterWarning[]`)
- [ ] T010 [P] Define `IClusterMonitoringAdapter` port in `src/NatsManager.Application/Modules/Monitoring/Ports/ClusterObservability/IClusterMonitoringAdapter.cs`: `Task<ClusterObservation> GetClusterObservationAsync(Guid environmentId, CancellationToken ct)` plus per-endpoint methods for `/healthz`, `/varz`, `/jsz`, `/routez`, `/gatewayz`, `/leafz`
- [ ] T011 [P] Define `IClusterObservationStore` port in `src/NatsManager.Application/Modules/Monitoring/Ports/ClusterObservability/IClusterObservationStore.cs`: get latest observation for environment, store observation, retain stale entries within configurable troubleshooting window (ephemeral, no SQLite)
- [ ] T012 Extend `src/NatsManager.Application/Modules/Monitoring/MonitoringOptions.cs` with cluster observability options: `ClusterPollingIntervalSeconds` (default 30), `ClusterEndpointTimeoutSeconds` (default 10, max 10 per plan), `StaleThresholdSeconds`, `MaxRetainedObservations`
- [ ] T013 Implement in-memory `ClusterObservationStore` in `src/NatsManager.Infrastructure/Monitoring/ClusterObservability/ClusterObservationStore.cs` implementing `IClusterObservationStore`: bounded ring buffer per environment, freshness transitions per data-model.md §5, no payload/JWT retention
- [ ] T014 Implement `NatsClusterMonitoringHttpAdapter` in `src/NatsManager.Infrastructure/Nats/ClusterObservability/NatsClusterMonitoringHttpAdapter.cs` implementing `IClusterMonitoringAdapter`: typed `HttpClient` with 10s timeout, parses `/varz`/`/jsz`/`/healthz`/`/routez`/`/gatewayz`/`/leafz`, masks/omits account/operator JWT fields, marks missing endpoints as `Unavailable`/`Partial` rather than failing the whole observation
- [ ] T015 Implement freshness/health derivation helper in `src/NatsManager.Application/Modules/Monitoring/Models/ClusterObservability/ClusterHealthDerivation.cs`: aggregates `ServerStatus` → `ClusterStatus`, derives counter rates from consecutive snapshots only (no unavailable baselines), derives `ObservationFreshness` per state-transitions table
- [ ] T016 [P] Register cluster observability services in `src/NatsManager.Web/Program.cs`: bind `MonitoringOptions`, register `IClusterMonitoringAdapter` (typed `HttpClient`), `IClusterObservationStore` (singleton), and any helpers in DI under existing Monitoring registration block
- [ ] T017 [P] Implement background `ClusterObservationPoller` in `src/NatsManager.Web/BackgroundServices/ClusterObservationPoller.cs`: per-environment polling honoring `ClusterPollingIntervalSeconds`, writes to `IClusterObservationStore`, isolates failures per environment
- [ ] T018 [P] Add shared frontend types in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/types.ts` mirroring data-model.md §1–4 (`ClusterObservation`, `ServerObservation`, `TopologyRelationship`, `ClusterTopologyGraph`, all enums); reuse existing freshness/data-source UI primitives from `src/NatsManager.Frontend/src/shared/`
- [ ] T019 [P] Add navigation entry under existing Monitoring section in `src/NatsManager.Frontend/src/shared/AppLayout.tsx` (or the equivalent Monitoring nav file in `src/NatsManager.Frontend/src/features/monitoring/`): "Cluster Observability" link routed to `/environments/:envId/monitoring/cluster`; respect existing JetStream-disabled and stale-environment conditional rules

**Checkpoint**: Foundation ready — models, ports, store, HTTP adapter, polling, DI, and frontend types/nav are in place

---

## Phase 3: User Story 1 — Understand cluster health at a glance (Priority: P1) 🎯 MVP

**Goal**: Operators see an environment-scoped cluster overview summarizing server count, degraded server count, JetStream availability, connection count, message rates, last observed time, and overall freshness/status.

**Independent Test**: Select an environment with reachable monitoring data and confirm the cluster overview shows status, freshness, server/degraded counts, JetStream availability, and last observed time without any other feature being open.

### Tests for User Story 1

- [ ] T020 [P] [US1] Unit tests for `ClusterHealthDerivation` in `tests/NatsManager.Application.Tests/Modules/Monitoring/ClusterObservability/ClusterHealthDerivationTests.cs`: aggregate `ClusterStatus` (Healthy/Degraded/Unavailable/Unknown), freshness transitions, counter-rate derivation refuses unavailable baselines (FR-CLU-002, FR-CLU-004)
- [ ] T021 [P] [US1] Unit tests for `GetClusterOverviewQuery` handler in `tests/NatsManager.Application.Tests/Modules/Monitoring/ClusterObservability/GetClusterOverviewQueryTests.cs`: returns latest observation, environment isolation (FR-CLU-009), unavailable-state response when store empty (FR-CLU-010), warning generation
- [ ] T022 [P] [US1] Integration tests for `NatsClusterMonitoringHttpAdapter` in `tests/NatsManager.Infrastructure.Tests/Nats/ClusterObservability/NatsClusterMonitoringHttpAdapterTests.cs` using `HttpMessageHandler` fixtures: parses `/varz`/`/jsz`/`/healthz`, marks `/jsz`-disabled as `JetStreamAvailable=null`, treats per-endpoint failure as `Partial`, redacts JWT fields (FR-CLU-008, FR-CLU-012)
- [ ] T023 [P] [US1] Contract tests for cluster overview endpoint in `tests/NatsManager.Web.Tests/Monitoring/ClusterObservability/ClusterOverviewEndpointTests.cs`: `GET /api/environments/{environmentId}/monitoring/cluster/overview` returns 200 with shape from api-contracts.md §1.1, 400 when monitoring not configured, 404 unknown environment, 503 when all endpoints fail
- [ ] T024 [P] [US1] Frontend component tests in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: `ClusterOverviewCard.test.tsx`, `ClusterFreshnessBadge.test.tsx`, `ClusterUnavailableState.test.tsx`; hook test in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/hooks/useClusterOverview.test.ts` (uses MSW)

### Implementation for User Story 1

- [ ] T025 [US1] Implement `GetClusterOverviewQuery` and handler in `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/GetClusterOverviewQuery.cs`: reads from `IClusterObservationStore`, returns `ClusterObservation` with derived `ClusterStatus`/freshness/warnings; emits unavailable state when store empty
- [ ] T026 [US1] Add cluster overview endpoint to existing `src/NatsManager.Web/Endpoints/MonitoringEndpoints.cs` (do not create a new endpoint class): `GET /api/environments/{environmentId:guid}/monitoring/cluster/overview` per api-contracts.md §1.1, including 400/404/503 problem details and `X-Data-Freshness` headers
- [ ] T027 [P] [US1] Implement `useClusterOverview` hook in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/hooks/useClusterOverview.ts`: TanStack Query, environment-scoped query key, polling interval matching `ClusterPollingIntervalSeconds`, preserves stale data on refresh
- [ ] T028 [P] [US1] Implement `ClusterOverviewCard` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/ClusterOverviewCard.tsx`: status, freshness, server count, degraded count, JetStream availability, connection count, in/out msg rates, last observed time; reuses `DataFreshnessIndicator` and `DataSourceBadge` from `src/NatsManager.Frontend/src/shared/`
- [ ] T029 [P] [US1] Implement `ClusterFreshnessBadge.tsx`, `ClusterWarningList.tsx`, and `ClusterUnavailableState.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: warning list with severity, unavailable guidance per FR-CLU-010
- [ ] T030 [US1] Implement `ClusterObservabilityPage.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/ClusterObservabilityPage.tsx`: composes overview card and unavailable-state guidance; tab placeholders for Servers (US2) and Topology (US3); reuses existing `EnvironmentContext`
- [ ] T031 [US1] Add lazy route `/environments/:envId/monitoring/cluster` in `src/NatsManager.Frontend/src/App.tsx` referencing `ClusterObservabilityPage` (route-based code splitting consistent with existing Monitoring routes)

**Checkpoint**: User Story 1 complete — cluster overview is observable end-to-end with freshness/unavailable states

---

## Phase 4: User Story 2 — Compare server-level metrics (Priority: P2)

**Goal**: Operators see a sortable, searchable, filterable list of servers with version, uptime, connections, slow consumers, memory/storage, message/byte rates, status, freshness, and per-server warnings; expanding a row reveals trend context.

**Independent Test**: Connect to a multi-server environment and confirm rows are sortable by connection count, slow-consumer warnings are highlighted, and expanding a server row reveals recent trend context.

### Tests for User Story 2

- [ ] T032 [P] [US2] Unit tests for warning-state derivation in `tests/NatsManager.Application.Tests/Modules/Monitoring/ClusterObservability/ServerWarningRulesTests.cs`: high slow consumers, high connection pressure, storage pressure, stale freshness (FR-CLU-005)
- [ ] T033 [P] [US2] Contract tests for server list shape inside overview response in `tests/NatsManager.Web.Tests/Monitoring/ClusterObservability/ClusterOverviewServersContractTests.cs`: `servers[]` matches data-model.md §2, missing fields serialize as `null`/unavailable
- [ ] T034 [P] [US2] Frontend component tests in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: `ClusterServerList.test.tsx` (sorting by connections/slow consumers/version, search/filter, virtualization for ≥250 rows), `ServerWarningBadge.test.tsx`, `ServerRowExpansion.test.tsx`

### Implementation for User Story 2

- [ ] T035 [US2] Extend `GetClusterOverviewQuery` handler in `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/GetClusterOverviewQuery.cs` to populate per-server warnings and `MetricStates` using thresholds from `MonitoringOptions` (slow-consumer count, connection pressure %, storage pressure %); add unit-test seam for thresholds
- [ ] T036 [P] [US2] Implement `ClusterServerList.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/ClusterServerList.tsx`: reuses existing `ResourceListView` from `src/NatsManager.Frontend/src/shared/`, virtualization (`@tanstack/react-virtual`) for 250+ rows, sortable columns (status, name/identifier, version, connections, slow consumers, memory, storage, in/out msg rate, last observed)
- [ ] T037 [P] [US2] Implement `ServerWarningBadge.tsx` and `ServerMetricCell.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: per-metric `Live/Stale/Derived/Unavailable` indicator (FR-CLU-004), warning highlighting (FR-CLU-005)
- [ ] T038 [P] [US2] Implement `ServerRowExpansion.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: trend context (in/out msg rate, connection pressure) derived from retained observations in `IClusterObservationStore` via existing freshness window; falls back to "trend unavailable" when only one snapshot exists
- [ ] T039 [P] [US2] Implement `useClusterServers` selector hook in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/hooks/useClusterServers.ts`: derives server rows + warnings from `useClusterOverview` data without re-fetch; supports local search/filter callbacks (FR-CLU-006)
- [ ] T040 [US2] Wire `ClusterServerList` into the **Servers** tab of `ClusterObservabilityPage.tsx` and ensure environment-isolation guard prevents stale data from a different environment from rendering during environment switch

**Checkpoint**: User Story 2 complete — server-level comparison is sortable, filterable, and warning-aware for ≥250 servers

---

## Phase 5: User Story 3 — Inspect topology signals safely (Priority: P3)

**Goal**: Operators see safe route/gateway/leafnode/cluster-peer topology with status, freshness, direction, and external-relationship labels rendered via React Flow (`@xyflow/react`), with a tabular fallback and accessible node selection.

**Independent Test**: Connect to an environment exposing route/gateway/leaf metadata and verify nodes/edges render with status/freshness, external relationships are labeled, and account/operator JWT details are absent.

### Tests for User Story 3

- [ ] T041 [P] [US3] Unit tests for topology projection in `tests/NatsManager.Application.Tests/Modules/Monitoring/ClusterObservability/TopologyProjectionTests.cs`: deterministic `RelationshipId`, JWT exclusion, external-gateway labeling, single-node environment yields no comparison topology, missing endpoints produce `Partial`
- [ ] T042 [P] [US3] Integration tests for `/routez`, `/gatewayz`, `/leafz` parsing in `tests/NatsManager.Infrastructure.Tests/Nats/ClusterObservability/NatsClusterMonitoringTopologyTests.cs`: ingestion fixtures for each endpoint, partial-failure isolation, sensitive fields stripped
- [ ] T043 [P] [US3] Contract tests for topology endpoint in `tests/NatsManager.Web.Tests/Monitoring/ClusterObservability/ClusterTopologyEndpointTests.cs`: `GET /api/environments/{environmentId}/monitoring/cluster/topology` 200 shape per api-contracts.md §1.2, query filters (`types`, `status`, `includeStale`, `maxNodes`) bounded to ≤1000, 422 on invalid filters, 503 when no topology endpoints succeed
- [ ] T044 [P] [US3] Frontend tests in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: `ClusterTopologyFlow.test.tsx` (deterministic layout fixture, accessible node selection, keyboard nav), `ClusterTopologyLegend.test.tsx`, `ClusterTopologyTableFallback.test.tsx`, `ClusterTopologyFilters.test.tsx`, `ClusterTopologyEmptyState.test.tsx`

### Implementation for User Story 3

- [ ] T045 [US3] Implement `GetClusterTopologyQuery` and handler in `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/GetClusterTopologyQuery.cs`: returns bounded `ClusterTopologyGraph` projection per data-model.md §4, supports `types`/`status`/`includeStale`/`maxNodes` (default 250, max 1000), populates `omittedCounts`
- [ ] T046 [US3] Add topology endpoint to existing `src/NatsManager.Web/Endpoints/MonitoringEndpoints.cs`: `GET /api/environments/{environmentId:guid}/monitoring/cluster/topology` per api-contracts.md §1.2, including 422 validation and 503 when no topology endpoint succeeded
- [ ] T047 [P] [US3] Implement `useClusterTopology` hook in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/hooks/useClusterTopology.ts`: TanStack Query keyed by environment + filters; preserves previous graph during refresh
- [ ] T048 [P] [US3] Implement `ClusterTopologyFlow.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/ClusterTopologyFlow.tsx`: imports from `@xyflow/react` (depends on T001), wraps `<ReactFlow>` with `ReactFlowProvider`, deterministic layout helper, custom node/edge components per node `type` (`server`, `routePeer`, `gateway`, `leafnode`, `external`), keyboard-accessible node selection, pan/zoom/minimap
- [ ] T049 [P] [US3] Implement `ClusterTopologyLegend.tsx` and `ClusterTopologyFilters.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/`: filters for relationship type, status, freshness; legend explaining external/safe-metadata labeling (FR-CLU-007, FR-CLU-008)
- [ ] T050 [P] [US3] Implement `ClusterTopologyTableFallback.tsx` and `ClusterTopologyEmptyState.tsx` in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/`: tabular relationship list (single-node environment, partial/unavailable topology), single-node "comparison not available" guidance per spec edge case
- [ ] T051 [US3] Wire topology UI into the **Topology** tab of `ClusterObservabilityPage.tsx`, including filter state synchronization with URL query params and unsafe-relationship omitted-count surfacing

**Checkpoint**: User Story 3 complete — safe topology visualization operational with React Flow + table fallback

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Quality, accessibility, performance, and security hardening across all three stories. Reuse existing shared infrastructure rather than introducing parallel UI patterns.

- [ ] T052 [P] Verify environment isolation across all cluster endpoints/components in `tests/NatsManager.Web.Tests/Monitoring/ClusterObservability/EnvironmentIsolationTests.cs`: switching environments invalidates queries, no cross-environment merge (FR-CLU-009)
- [ ] T053 [P] Add JWT/payload safety regression tests in `tests/NatsManager.Infrastructure.Tests/Nats/ClusterObservability/SafeMetadataTests.cs`: golden fixtures with embedded JWT/payload fields confirm redaction (FR-CLU-008, FR-CLU-012, SC-CLU-005)
- [ ] T054 [P] Reuse existing `StaleDataBanner` from `src/NatsManager.Frontend/src/shared/StaleDataBanner.tsx` in `ClusterObservabilityPage.tsx` for stale/partial observations and degraded environments (FR-CLU-010)
- [ ] T055 [P] Audit accessibility in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/`: keyboard navigation for server list expansion and React Flow node selection, ARIA labels for status/freshness, screen-reader summary for topology graph
- [ ] T056 [P] Performance audit: assert local search/filter ≤ 300ms (Vitest perf assertion or React Profiler test) and topology layout for 250 servers ≤ 2s in `src/NatsManager.Frontend/src/features/monitoring/cluster-observability/components/ClusterTopologyFlow.test.tsx` (SC-CLU-004)
- [ ] T057 [P] Add structured logging tags to `NatsClusterMonitoringHttpAdapter` and `ClusterObservationPoller` in `src/NatsManager.Infrastructure/`: per-endpoint outcome (success/partial/failed), per-environment correlation id, no payload/JWT content
- [ ] T058 [P] Confirm bundle impact in `src/NatsManager.Frontend/`: run `npm run build`, document `@xyflow/react` chunk size, ensure cluster-observability chunk lazy-loads (initial bundle stays ≤ 300KB gzipped per existing constitution)
- [ ] T059 Run quickstart validation per `specs/copilot/cluster-observability/quickstart.md`: enable monitoring on a NATS server, register environment, open Cluster Observability, verify overview/server-list/topology behaviors and unavailable/partial states; fix issues found

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 (`@xyflow/react` install) MUST precede any frontend task that imports from `@xyflow/react` (T044, T048). T002–T004 are independent of T001 and can run in parallel.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational only.
- **User Story 2 (Phase 4)**: Depends on Foundational + US1 endpoint (extends overview response with per-server warnings/metric states).
- **User Story 3 (Phase 5)**: Depends on Foundational only; topology endpoint is independent of overview consumers.
- **Polish (Phase 6)**: Depends on US1–US3 being complete.

### Within Each User Story

- Tests written first (fail before implementation)
- Models/enums before adapters
- Adapters/queries before endpoints
- Hooks before components
- Page integration last

### Parallel Opportunities

- **Phase 1**: T001, T002, T003, T004 all parallel
- **Phase 2**: T006–T008 parallel (DTOs); T010–T011 parallel (ports); T016–T019 parallel (DI/poller/types/nav)
- **Phase 3 (US1)**: T020–T024 parallel (tests); T027–T029 parallel (frontend hooks/components)
- **Phase 4 (US2)**: T032–T034 parallel (tests); T036–T039 parallel (components/hook)
- **Phase 5 (US3)**: T041–T044 parallel (tests); T047–T050 parallel (frontend hook/components/fallback)
- **Phase 6**: T052–T058 parallel

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (`@xyflow/react` pin, folders)
2. Phase 2: Foundational
3. Phase 3: US1 cluster overview
4. **STOP and validate**: SC-CLU-001 (≤10s identification), SC-CLU-003 (freshness within polling interval)

### Incremental Delivery

- US1 → cluster-at-a-glance (MVP)
- US2 → server comparison
- US3 → safe topology visualization
- Polish → security/accessibility/performance hardening

### Parallel Team Strategy

After Foundational:

- Developer A: US1 → Polish security
- Developer B: US2 (server list/warnings/trend)
- Developer C: US3 (topology endpoint + React Flow)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- All endpoints are read-only and require authenticated session + selected environment context (FR-CLU-001, FR-CLU-009)
- All NATS monitoring HTTP calls are bounded at 10s (FR-CLU-010)
- Account/operator JWT and payload content are never persisted or surfaced (FR-CLU-008, FR-CLU-012, SC-CLU-005)
- This feature **extends** the existing Monitoring module — do not create a parallel Monitoring/Cluster bounded context
- `@xyflow/react` is a new pinned dependency added in T001; legacy `reactflow` package must not be introduced

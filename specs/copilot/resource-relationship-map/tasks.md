# Tasks: Resource Relationship Map

**Input**: Design documents from `/specs/copilot/resource-relationship-map/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Included — repository convention requires xUnit v3 + NSubstitute (backend) and Vitest + React Testing Library (frontend) with contract tests at the API boundary.

**Organization**: Tasks grouped by user story for independent implementation and testing.

**Overlap / main-state note**: `origin/main` does not yet contain this feature folder, does not contain `@xyflow/react`/`reactflow`, and only has generic relationship/topology references in existing root guidance. All tasks below **compose** existing modules — `CoreNats`, `JetStream`, `KeyValue`, `ObjectStore`, `Services`, `Monitoring`, `Search`, and any in-app `Alerts`/`Events` surfaces — through a new read-model-only `Relationships` module. They **must not** duplicate or fork those modules' resource ownership, search/indexing, or alert handling. Map entry points are added to existing resource detail pages (`src/NatsManager.Frontend/src/features/jetstream/`, `kv/`, `objectstore/`, `services/`, `corenats/`, `monitoring/`).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the new frontend dependency and create the empty module folders this feature will populate.

- [ ] T001 [P] Pin React Flow as a new frontend dependency in `src/NatsManager.Frontend/package.json` under `dependencies`: add `"@xyflow/react": "12.x"` (use a single resolved minor/patch version, e.g. `12.3.6`); run `npm install` to update `src/NatsManager.Frontend/package-lock.json`. **This task MUST complete before any task that imports from `@xyflow/react`.** If the cluster-observability feature has already added this pin, reuse the same version and skip the package edit (still validate lockfile).
- [ ] T002 [P] Create backend folder skeleton: `src/NatsManager.Application/Modules/Relationships/Models/`, `src/NatsManager.Application/Modules/Relationships/Ports/`, `src/NatsManager.Application/Modules/Relationships/Queries/`, `src/NatsManager.Infrastructure/Relationships/` (placeholders only)
- [ ] T003 [P] Create frontend folder skeleton: `src/NatsManager.Frontend/src/features/relationships/`, `src/NatsManager.Frontend/src/features/relationships/hooks/`, `src/NatsManager.Frontend/src/features/relationships/components/`
- [ ] T004 [P] Create test folder skeleton: `tests/NatsManager.Application.Tests/Modules/Relationships/`, `tests/NatsManager.Infrastructure.Tests/Relationships/`, `tests/NatsManager.Web.Tests/Relationships/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Read-model types, enums, ports, projection engine, and per-source relationship adapters that all three user stories depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 [US-shared] Implement relationship enums in `src/NatsManager.Application/Modules/Relationships/Models/Enums.cs`: `ResourceType`, `RelationshipType`, `RelationshipDirection`, `ObservationKind`, `RelationshipConfidence`, `RelationshipFreshness`, `ResourceHealthStatus`, `RelationshipSourceModule` per data-model.md §7
- [ ] T006 [P] Implement `FocalResource` record in `src/NatsManager.Application/Modules/Relationships/Models/FocalResource.cs` per data-model.md §1 (validation: `EnvironmentId`/`ResourceType`/`ResourceId` required, environment match)
- [ ] T007 [P] Implement `ResourceNode` record in `src/NatsManager.Application/Modules/Relationships/Models/ResourceNode.cs` per data-model.md §2 (deterministic `NodeId = environment:type:id`, `SafeMetadata` excludes payload/JWT/credentials)
- [ ] T008 [P] Implement `RelationshipEvidence` record in `src/NatsManager.Application/Modules/Relationships/Models/RelationshipEvidence.cs` per data-model.md §4
- [ ] T009 [P] Implement `RelationshipEdge` record in `src/NatsManager.Application/Modules/Relationships/Models/RelationshipEdge.cs` per data-model.md §3 (deterministic `EdgeId`, inferred edges must include evidence + confidence, `Direction=Unknown` when not safely known)
- [ ] T010 [P] Implement `MapFilter` record + validator in `src/NatsManager.Application/Modules/Relationships/Models/MapFilter.cs` per data-model.md §6: `Depth` 1–3, `MaxNodes` 1–500, `MaxEdges` 1–2000, defaults (depth=1, maxNodes=100, maxEdges=500, minimumConfidence=Low, includeInferred=true, includeStale=true)
- [ ] T011 [P] Implement `OmittedCounts` record in `src/NatsManager.Application/Modules/Relationships/Models/OmittedCounts.cs` (`filteredNodes`, `filteredEdges`, `collapsedNodes`, `collapsedEdges`, `unsafeRelationships`)
- [ ] T012 Implement `RelationshipMap` record in `src/NatsManager.Application/Modules/Relationships/Models/RelationshipMap.cs` per data-model.md §5 (composes T006–T011)
- [ ] T013 Define `IRelationshipSource` port in `src/NatsManager.Application/Modules/Relationships/Ports/IRelationshipSource.cs`: `RelationshipSourceModule Module { get; }`, `Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(FocalResource focal, MapFilter filters, CancellationToken ct)`, `Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)`
- [ ] T014 [P] Define `IFocalResourceResolver` port in `src/NatsManager.Application/Modules/Relationships/Ports/IFocalResourceResolver.cs`: resolves a `FocalResource` (existence, displayName, detailRoute) by `(EnvironmentId, ResourceType, ResourceId)` using existing module queries; returns `null` for missing focal (drives 404)
- [ ] T015 Implement `RelationshipProjectionService` in `src/NatsManager.Infrastructure/Relationships/RelationshipProjectionService.cs`: orchestrates registered `IRelationshipSource` instances, performs bounded BFS up to `Depth`, applies type/health/confidence/inferred/stale filters, enforces `MaxNodes`/`MaxEdges`, populates `OmittedCounts`, never crosses environments, removes unsafe relationships and increments `unsafeRelationships`
- [ ] T016 [P] Implement `JetStreamRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/JetStreamRelationshipSource.cs` consuming existing `IJetStreamAdapter` from `src/NatsManager.Application/Modules/JetStream/Ports/`: stream→subject (`UsesSubject`), stream→consumer (`Contains`), consumer→stream (`BackedByStream`), evidence type `ConsumerParent`/`StreamSubject`
- [ ] T017 [P] Implement `KeyValueRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/KeyValueRelationshipSource.cs` consuming existing `IKvStoreAdapter`: bucket→backing stream (`BackedByStream`), key→bucket (`Contains`)
- [ ] T018 [P] Implement `ObjectStoreRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/ObjectStoreRelationshipSource.cs` consuming existing `IObjectStoreAdapter`: bucket→backing stream (`BackedByStream`), object→bucket (`Contains`)
- [ ] T019 [P] Implement `ServicesRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/ServicesRelationshipSource.cs` consuming existing `IServiceDiscoveryAdapter`: service→endpoint (`Contains`), endpoint→subject (`UsesSubject`), service group/version (`DependsOn`); confidence: `Observed`/`High`
- [ ] T020 [P] Implement `CoreNatsRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/CoreNatsRelationshipSource.cs` consuming existing `ICoreNatsAdapter`: subject→subscription/service usage where safely available, server→subject hosting (`HostedOn`); inferred edges flagged with `ObservationKind=Inferred` and confidence per evidence quality
- [ ] T021 [P] Implement `MonitoringRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/MonitoringRelationshipSource.cs` consuming existing Monitoring queries: server→hosted resources (`HostedOn`), stale/degraded server signals; integrates with cluster-observability data when available but does NOT depend on it
- [ ] T022 [P] Implement `AlertsRelationshipSource` in `src/NatsManager.Infrastructure/Relationships/Sources/AlertsRelationshipSource.cs`: alert/event→affected resource (`AffectedBy`, `RelatedEvent`) using safe identifiers from existing in-app alerts/events surfaces (use `Dashboard`/audit alert read models if no dedicated Alerts module yet); evidence type `AlertAffectedResource`
- [ ] T023 Implement `IFocalResourceResolver` adapter in `src/NatsManager.Infrastructure/Relationships/FocalResourceResolver.cs` dispatching to existing module queries (Stream/Consumer/Subject/Service/KvBucket/KvKey/ObjectBucket/Object/Server/Alert/Event); returns existing detail route paths
- [ ] T024 Register relationships services in `src/NatsManager.Web/Program.cs`: register all `IRelationshipSource` implementations (T016–T022), `IFocalResourceResolver` (T023), `RelationshipProjectionService` as the implementation backing `IRelationshipSource` orchestration; add to existing module DI registration block
- [ ] T025 [P] Add shared frontend types in `src/NatsManager.Frontend/src/features/relationships/types.ts` mirroring data-model.md §1–7 (`RelationshipMap`, `ResourceNode`, `RelationshipEdge`, `RelationshipEvidence`, `MapFilter`, `OmittedCounts`, all enums)
- [ ] T026 [P] Add shared "View Relationship Map" entry-point button component in `src/NatsManager.Frontend/src/features/relationships/components/OpenRelationshipMapButton.tsx`: navigates to `/environments/:envId/relationships?resourceType=...&resourceId=...`, reused by all resource detail pages

**Checkpoint**: Foundation ready — relationship read model, projection engine, all per-source adapters, focal resolver, and shared frontend types/entry-point in place

---

## Phase 3: User Story 1 — Visualize relationships around a resource (Priority: P1) 🎯 MVP

**Goal**: From a supported resource detail page, an operator opens a focal-resource relationship map showing connected resources, observed directionality, health, and confidence/freshness — within environment context.

**Independent Test**: Open a stream with known consumers/subjects, click "View Relationship Map", and verify the map shows the focal stream + connected consumers/subjects with health and freshness labels and environment context preserved.

### Tests for User Story 1

- [ ] T027 [P] [US1] Unit tests for `RelationshipProjectionService` in `tests/NatsManager.Application.Tests/Modules/Relationships/RelationshipProjectionServiceTests.cs`: bounded BFS at depth=1, environment isolation (FR-RRM-009), unsafe relationship removal increments `unsafeRelationships` (FR-RRM-008), inferred edges include evidence + confidence (FR-RRM-004)
- [ ] T028 [P] [US1] Unit tests per source in `tests/NatsManager.Application.Tests/Modules/Relationships/Sources/`: `JetStreamRelationshipSourceTests.cs`, `KeyValueRelationshipSourceTests.cs`, `ObjectStoreRelationshipSourceTests.cs`, `ServicesRelationshipSourceTests.cs`, `CoreNatsRelationshipSourceTests.cs`, `MonitoringRelationshipSourceTests.cs`, `AlertsRelationshipSourceTests.cs` — using NSubstitute mocks of existing module ports; verify deterministic node/edge ids and safe-metadata-only evidence
- [ ] T029 [P] [US1] Unit tests for `FocalResourceResolver` in `tests/NatsManager.Infrastructure.Tests/Relationships/FocalResourceResolverTests.cs`: resolves each supported `ResourceType`, returns null for missing focal, refuses cross-environment lookups
- [ ] T030 [P] [US1] Contract tests for `GET /api/environments/{environmentId}/relationships/map` in `tests/NatsManager.Web.Tests/Relationships/RelationshipMapEndpointTests.cs`: 200 shape per api-contracts.md §1.1, 400 missing focal, 404 missing focal resource, 422 invalid filter bounds, 503 when all sources unavailable, environment-scoped query key
- [ ] T031 [P] [US1] Contract tests for `GET /api/environments/{environmentId}/relationships/nodes/{nodeId}` in `tests/NatsManager.Web.Tests/Relationships/RelationshipNodeEndpointTests.cs`: 200 shape per api-contracts.md §1.2, 404, environment isolation
- [ ] T032 [P] [US1] Frontend tests in `src/NatsManager.Frontend/src/features/relationships/`: `ResourceRelationshipMapPage.test.tsx`, `RelationshipFlow.test.tsx` (deterministic layout fixture, focal node highlighted, status/freshness on nodes/edges, keyboard-accessible node selection), `RelationshipEvidencePanel.test.tsx`, `OpenRelationshipMapButton.test.tsx`, `hooks/useResourceRelationshipMap.test.ts` (uses MSW)

### Implementation for User Story 1

- [ ] T033 [US1] Implement `GetResourceRelationshipMapQuery` and handler in `src/NatsManager.Application/Modules/Relationships/Queries/GetResourceRelationshipMapQuery.cs`: validates filters (T010), resolves focal via `IFocalResourceResolver`, calls `RelationshipProjectionService`, returns `RelationshipMap` with `OmittedCounts`
- [ ] T034 [US1] Implement `GetRelationshipNodeQuery` and handler in `src/NatsManager.Application/Modules/Relationships/Queries/GetRelationshipNodeQuery.cs`: resolves single node via `IFocalResourceResolver`, returns navigation/status payload per api-contracts.md §1.2
- [ ] T035 [US1] Implement `RelationshipMapEndpoints.cs` in `src/NatsManager.Web/Endpoints/RelationshipMapEndpoints.cs`: `GET /api/environments/{environmentId:guid}/relationships/map` (query parameters per api-contracts.md §1.1), `GET /api/environments/{environmentId:guid}/relationships/nodes/{nodeId}`; problem details for 400/404/422/503; `X-Data-Freshness` header; map endpoint registration into existing `Program.cs` minimal-API pipeline
- [ ] T036 [P] [US1] Implement `useResourceRelationshipMap` hook in `src/NatsManager.Frontend/src/features/relationships/hooks/useResourceRelationshipMap.ts` per api-contracts.md §2: TanStack Query keyed by `(environmentId, resourceType, resourceId, filters)`, does not fetch until all required inputs present, preserves previous graph during refresh, exposes `recenter(node)` and `openDetails(node)`
- [ ] T037 [P] [US1] Implement `useRelationshipNode` hook in `src/NatsManager.Frontend/src/features/relationships/hooks/useRelationshipNode.ts` for node-detail panel
- [ ] T038 [P] [US1] Implement `RelationshipFlow.tsx` in `src/NatsManager.Frontend/src/features/relationships/RelationshipFlow.tsx`: imports from `@xyflow/react` (depends on T001), `<ReactFlowProvider>`, deterministic layout for nodes/edges from `RelationshipMap`, custom node components per `ResourceType` (focal highlighted), edge styling per `ObservationKind`/`Confidence`/`Status`, keyboard-accessible node selection, pan/zoom/minimap/controls
- [ ] T039 [P] [US1] Implement `RelationshipEvidencePanel.tsx` in `src/NatsManager.Frontend/src/features/relationships/RelationshipEvidencePanel.tsx`: shows selected node/edge identity, status, freshness, observed/inferred badge, confidence, evidence list with safe fields; reuses `DataFreshnessIndicator` from `src/NatsManager.Frontend/src/shared/`
- [ ] T040 [US1] Implement `ResourceRelationshipMapPage.tsx` in `src/NatsManager.Frontend/src/features/relationships/ResourceRelationshipMapPage.tsx`: reads `environmentId`/`resourceType`/`resourceId` from URL, composes `RelationshipFlow` + `RelationshipEvidencePanel`, includes empty/missing-focal/partial/unavailable states, preserves existing `EnvironmentContext`
- [X] T041 [US1] Add lazy route `/environments/:envId/relationships` to `src/NatsManager.Frontend/src/App.tsx` (route-based code splitting), accepting `resourceType`/`resourceId` as query params

**Checkpoint**: User Story 1 complete — focal-resource map renders end-to-end with evidence/freshness/health and environment-scoped routing

---

## Phase 4: User Story 2 — Traverse dependencies during an incident (Priority: P2)

**Goal**: From an alert, event, or unhealthy resource, an operator follows links to adjacent resources, sees warning states without opening each detail, and recenters the map on a connected node while keeping environment context.

**Independent Test**: Open the map starting from an unhealthy consumer (or alert pointing to a consumer) and confirm the consumer is highlighted, neighboring stream/subjects/services show warning states inline, and recentering on a neighbor preserves environment context.

### Tests for User Story 2

- [ ] T042 [P] [US2] Unit tests for alert/event→resource edge construction in `tests/NatsManager.Application.Tests/Modules/Relationships/Sources/AlertsRelationshipSourceTraversalTests.cs`: alert focal returns affected resources (FR-RRM-012), missing safe identifiers omit edges and increment `unsafeRelationships`
- [ ] T043 [P] [US2] Unit tests for warning propagation in `tests/NatsManager.Application.Tests/Modules/Relationships/RelationshipMapWarningsTests.cs`: neighbor warning states surface on `ResourceNode.Status` (FR-RRM-003), focal highlight flag set
- [ ] T044 [P] [US2] Frontend tests in `src/NatsManager.Frontend/src/features/relationships/`: `RelationshipFlow.recentering.test.tsx` (recenter preserves environment context, updates URL/query state, does not lose previous selection until new graph arrives), `AlertHighlight.test.tsx` (alert/event focal renders affected resources highlighted), `WarningOverlay.test.tsx` (neighbor warning indicators)
- [ ] T045 [P] [US2] Contract tests in `tests/NatsManager.Web.Tests/Relationships/RelationshipMapAlertFocalTests.cs`: focal `resourceType=Alert`/`Event` returns affected-resource neighborhood with safe evidence

### Implementation for User Story 2

- [ ] T046 [US2] Extend `RelationshipProjectionService` in `src/NatsManager.Infrastructure/Relationships/RelationshipProjectionService.cs` to mark focal node + propagate immediate-neighbor warning summary onto `ResourceNode.Status` for incident traversal (without altering owning module state)
- [X] T047 [P] [US2] Implement `RelationshipFlow` recenter behavior in `src/NatsManager.Frontend/src/features/relationships/RelationshipFlow.tsx`: `recenter(node)` updates URL (resourceType/resourceId) and triggers `useResourceRelationshipMap` re-query; previous graph kept on screen during refresh; clear focal-change announcement for screen readers
- [X] T048 [P] [US2] Implement `WarningOverlay.tsx` and `AlertHighlight.tsx` in `src/NatsManager.Frontend/src/features/relationships/components/`: visual emphasis for warning/degraded/stale neighbors, alert/event nodes rendered with distinct iconography
- [X] T049 [P] [US2] Implement `OpenDetailsButton.tsx` in `src/NatsManager.Frontend/src/features/relationships/components/`: navigates to `node.detailRoute` from existing module pages; degrades gracefully when `detailRoute=null`
- [X] T050 [US2] Wire entry-point button (`OpenRelationshipMapButton` from T026) into existing resource detail pages **without duplicating their logic**:
  - `src/NatsManager.Frontend/src/features/jetstream/components/StreamDetail.tsx` (Stream)
  - `src/NatsManager.Frontend/src/features/jetstream/components/ConsumerDetail.tsx` (Consumer)
  - `src/NatsManager.Frontend/src/features/corenats/components/SubjectExplorer.tsx` (Subject)
  - `src/NatsManager.Frontend/src/features/services/components/ServiceDetail.tsx` (Service)
  - `src/NatsManager.Frontend/src/features/kv/components/KvBucketDetail.tsx` and `KvKeyDetail.tsx`
  - `src/NatsManager.Frontend/src/features/objectstore/components/ObjectBucketDetail.tsx` and `ObjectDetail.tsx`
  - `src/NatsManager.Frontend/src/features/monitoring/` server detail (cluster server row → "View Relationship Map")
  - Existing alert/event detail surfaces in `src/NatsManager.Frontend/src/features/dashboard/` or `audit/`
- [ ] T051 [US2] Add corresponding entry-point tests `*EntryPoint.test.tsx` next to each updated detail component to verify the button passes correct `resourceType`/`resourceId` and preserves environment context

**Checkpoint**: User Story 2 complete — traversal from alerts/unhealthy resources is end-to-end with warning visibility and recentering

---

## Phase 5: User Story 3 — Filter and simplify large maps (Priority: P3)

**Goal**: Operators filter maps by resource type, relationship type, health state, observed-vs-inferred, minimum confidence, and depth from the focal resource. Empty-filter states and collapsed-branch counts are explicit.

**Independent Test**: Open a map in an environment with >100 related nodes, set depth=1 + filter to unhealthy resources, and verify only direct unhealthy neighbors render with omitted counts visible; clearing filters shows the full bounded map.

### Tests for User Story 3

- [ ] T052 [P] [US3] Unit tests for `MapFilter` validation in `tests/NatsManager.Application.Tests/Modules/Relationships/MapFilterValidationTests.cs`: depth 1–3, maxNodes 1–500, maxEdges 1–2000, defaults applied, invalid values produce 422 mapping
- [ ] T053 [P] [US3] Unit tests for filter application in `tests/NatsManager.Application.Tests/Modules/Relationships/RelationshipProjectionFilterTests.cs`: type/health/confidence/inferred/stale filters, `OmittedCounts.collapsedNodes`/`collapsedEdges` populated correctly, direct unhealthy neighbors retained even at deeper collapse (per research.md §5)
- [ ] T054 [P] [US3] Contract tests for filter combinations in `tests/NatsManager.Web.Tests/Relationships/RelationshipMapFilterTests.cs`: comma-separated `resourceTypes`/`relationshipTypes`/`healthStates`, `minimumConfidence` enum, boundary `maxNodes`/`maxEdges`, 422 on invalid combinations
- [ ] T055 [P] [US3] Frontend tests in `src/NatsManager.Frontend/src/features/relationships/`: `RelationshipFilters.test.tsx` (depth slider 1–3, type/relationship/health multi-select, observed/inferred toggle, confidence threshold, stale toggle, max nodes/edges inputs), `EmptyFilterState.test.tsx`, `CollapsedBranchCount.test.tsx`

### Implementation for User Story 3

- [ ] T056 [US3] Extend `RelationshipProjectionService` in `src/NatsManager.Infrastructure/Relationships/RelationshipProjectionService.cs` to enforce all filters from `MapFilter` and emit accurate `OmittedCounts.collapsedNodes`/`collapsedEdges` per research.md §5 defaults; ensure direct unhealthy neighbors are retained even when lower-confidence branches are collapsed
- [X] T057 [P] [US3] Implement `RelationshipFilters.tsx` in `src/NatsManager.Frontend/src/features/relationships/RelationshipFilters.tsx`: depth, resource types, relationship types, health states, observed/inferred, minimum confidence, include stale, max nodes/edges; updates URL query params so filters are shareable and deterministic for tests
- [X] T058 [P] [US3] Implement `EmptyFilterState.tsx` and `CollapsedBranchCount.tsx` in `src/NatsManager.Frontend/src/features/relationships/components/`: explicit empty-filter messaging (FR-RRM, US3 acceptance #3), summary of collapsed counts with "show more" affordance that increases `maxNodes`/`maxEdges` within bounds
- [X] T059 [US3] Wire `RelationshipFilters` into `ResourceRelationshipMapPage.tsx`; ensure `useResourceRelationshipMap` query key includes filter values; ensure performance budget for ≤500 available relationships rendering bounded neighborhood ≤ 2s (SC-RRM-003)

**Checkpoint**: User Story 3 complete — filtering, depth control, and explicit empty/collapsed states operational

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Quality, accessibility, performance, security, and documentation hardening.

- [ ] T060 [P] Add cross-environment rejection tests in `tests/NatsManager.Web.Tests/Relationships/EnvironmentIsolationTests.cs`: focal resource in environment A cannot reference nodes from environment B unless represented as `External` with safe metadata (FR-RRM-009)
- [ ] T061 [P] Add JWT/payload safety regression tests in `tests/NatsManager.Application.Tests/Modules/Relationships/SafeMetadataTests.cs`: golden fixtures with embedded JWT/payload fields confirm exclusion from `SafeMetadata`/`Evidence.SafeFields` (FR-RRM-008, FR-RRM-010, SC-RRM-005)
- [ ] T062 [P] Reuse existing `StaleDataBanner` from `src/NatsManager.Frontend/src/shared/StaleDataBanner.tsx` in `ResourceRelationshipMapPage.tsx` for partial/stale relationship data
- [ ] T063 [P] Audit accessibility in `src/NatsManager.Frontend/src/features/relationships/`: keyboard-accessible node/edge selection in React Flow, ARIA labels on status/confidence badges, focus management on recenter, screen-reader summary of nodes/edges count
- [ ] T064 [P] Performance audit: verify SC-RRM-003 (initial bounded neighborhood ≤ 2s for up to 500 relationships) and ≤300ms local filter response in `src/NatsManager.Frontend/src/features/relationships/RelationshipFlow.test.tsx` using deterministic fixture; verify backend p95 ≤ 1s for bounded queries via `tests/NatsManager.Web.Tests/Relationships/RelationshipMapPerformanceTests.cs` (skip in CI by default; document local run)
- [ ] T065 [P] Add structured logging in `RelationshipProjectionService` and endpoints: per-source success/failure, omitted-relationship reasons, per-environment correlation id, no payload/JWT content
- [ ] T066 [P] Confirm bundle impact in `src/NatsManager.Frontend/`: run `npm run build`, verify `relationships` chunk lazy-loads and shares `@xyflow/react` chunk with cluster-observability if also installed (initial bundle ≤ 300KB gzipped per existing constitution)
- [ ] T067 [P] Document map entry points and supported resource types in `src/NatsManager.Frontend/src/features/relationships/README.md` (developer-facing only; no end-user docs)
- [ ] T068 Run quickstart validation per `specs/copilot/resource-relationship-map/quickstart.md`: open map from a stream / consumer / service / KV / Object Store / server / alert; recenter; filter; verify SC-RRM-001 (≤2 clicks), SC-RRM-002 (≤30s), SC-RRM-004 (≥95% relationships labeled), SC-RRM-005 (no unsafe cross-environment data); fix issues found

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 (`@xyflow/react` install) MUST precede any frontend task that imports from `@xyflow/react` (T038, T044, T047, T055, T057, T064). T002–T004 are independent of T001 and can run in parallel.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational only. Required for US2 and US3.
- **User Story 2 (Phase 4)**: Depends on US1 (extends projection + map page with traversal/recentering and entry points).
- **User Story 3 (Phase 5)**: Depends on US1 (extends filter behavior on the same projection + page). May proceed in parallel with US2 once US1 is complete.
- **Polish (Phase 6)**: Depends on US1–US3 being complete.

### Within Each User Story

- Tests written first (fail before implementation)
- Models/enums before adapters
- Adapters/queries before endpoints
- Hooks before components
- Page integration last

### Parallel Opportunities

- **Phase 1**: T001, T002, T003, T004 all parallel
- **Phase 2**: T006–T011 parallel (DTOs); T013–T014 parallel (ports); T016–T022 parallel (per-source adapters); T025–T026 parallel (frontend types/entry-point)
- **Phase 3 (US1)**: T027–T032 parallel (tests); T036–T039 parallel (frontend hooks/components)
- **Phase 4 (US2)**: T042–T045 parallel (tests); T047–T049 parallel (frontend behaviors); T050 entry-point wiring per detail page is parallelizable per file
- **Phase 5 (US3)**: T052–T055 parallel (tests); T057–T058 parallel (frontend filter components)
- **Phase 6**: T060–T067 parallel

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (`@xyflow/react` pin, folders)
2. Phase 2: Foundational
3. Phase 3: US1 focal-resource map
4. **STOP and validate**: SC-RRM-001 (≤2 clicks), SC-RRM-004 (≥95% relationships labeled)

### Incremental Delivery

- US1 → focal-resource map (MVP)
- US2 → traversal + alert/event integration + entry points across detail pages
- US3 → filtering, depth control, collapsed-branch counts
- Polish → security/accessibility/performance hardening

### Parallel Team Strategy

After Foundational:

- Developer A: US1 → Polish security
- Developer B: US2 (traversal + entry-point wiring across detail pages)
- Developer C: US3 (filtering)

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- All endpoints are read-only and require authenticated session + selected environment context (FR-RRM-001, FR-RRM-009)
- Account/operator JWT and payload content are never persisted or surfaced (FR-RRM-008, FR-RRM-010, SC-RRM-005)
- This feature **composes** existing `CoreNats`, `JetStream`, `KeyValue`, `ObjectStore`, `Services`, `Monitoring`, `Search`, and `Alerts/Events` modules through a new read-model-only `Relationships` module — do not duplicate or fork those modules' resource ownership, search/indexing, or alert handling
- Map entry points are added to existing resource detail pages — do not create parallel detail pages
- `@xyflow/react` is a new pinned dependency added in T001; if cluster-observability has already pinned it, reuse the same version. Legacy `reactflow` package must not be introduced

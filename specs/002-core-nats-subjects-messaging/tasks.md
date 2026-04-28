# Tasks: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Input**: Design documents from `/specs/002-core-nats-subjects-messaging/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/api-contracts.md ✅, quickstart.md ✅

**Tests**: Included — spec requires xUnit v3 + MTP v2 (backend), Vitest + React Testing Library (frontend), Playwright (E2E); 80% new-code coverage target.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in all descriptions

---

## Phase 1: Shared Foundation (Blocking Prerequisites)

**Purpose**: Model, port, and infrastructure changes shared by all three user stories — MUST be complete before any story work begins.

**⚠️ CRITICAL**: No user story tasks can begin until this phase is complete.

- [x] T001 Add `PayloadFormat` enum and `NatsLiveMessage` record to `src/NatsManager.Application/Modules/CoreNats/Models/CoreNatsModels.cs`
- [x] T002 Update `ICoreNatsAdapter` in `src/NatsManager.Application/Modules/CoreNats/Ports/ICoreNatsAdapter.cs`: extend `PublishAsync` signature with `IReadOnlyDictionary<string, string>? headers = null, string? replyTo = null`; add `IAsyncEnumerable<NatsLiveMessage> SubscribeAsync(Guid environmentId, string subject, CancellationToken cancellationToken = default)`
- [x] T003 Add `CoreNatsMonitoringOptions` class in `src/NatsManager.Infrastructure/Configuration/CoreNatsMonitoringOptions.cs` with `DefaultPort` (int, 8222) and `HttpTimeout` (TimeSpan, 3s) properties and `SectionName = "CoreNats:Monitoring"` constant
- [x] T004 Register `CoreNatsMonitoringOptions` and `IHttpClientFactory` in `src/NatsManager.Web/Program.cs`: `builder.Services.Configure<CoreNatsMonitoringOptions>(builder.Configuration.GetSection(CoreNatsMonitoringOptions.SectionName))` and `builder.Services.AddHttpClient()`
- [x] T005 [P] Add `CoreNats:Monitoring:DefaultPort` and `CoreNats:Monitoring:HttpTimeout` config entries to `src/NatsManager.Web/appsettings.json` and `src/NatsManager.Web/appsettings.Development.json`
- [x] T006 [P] Fix `NatsSubjectInfo` type in `src/NatsManager.Frontend/src/features/corenats/types.ts`: rename `name`→`subject`, `messageCount`→`subscriptions`; add `PayloadFormat`, `PublishRequest`, `NatsLiveMessage` types per data-model.md

**Checkpoint**: Foundation ready — all shared types, ports, and configuration in place; user story work can now begin in parallel

---

## Phase 2: User Story 1 — Browse Active Subjects (Priority: P1) 🎯 MVP

**Goal**: `GET /subjects` returns real subscription data from NATS HTTP monitoring; a filterable subject table renders on the Core NATS page with auto-refresh and graceful degradation.

**Independent Test**: Navigate to Core NATS, confirm the subject table renders with at least one entry when a subscriber is active, type a filter string and confirm the list reduces to matching rows, and disable the monitoring endpoint to confirm an informational placeholder appears while the rest of the page stays functional.

### Tests for User Story 1

- [x] T007 [P] [US1] Unit tests for `ListSubjectsAsync` in `tests/NatsManager.Application.Tests/Modules/CoreNats/CoreNatsQueryCommandTests.cs`: test `GetSubjectsQueryHandler` returns subject list; test returns empty list when adapter returns empty
- [x] T008 [P] [US1] Integration test for `ListSubjectsAsync` in `tests/NatsManager.Integration.Tests/Nats/CoreNatsAdapterTests.cs`: verify method returns a list (not null) against real NATS server
- [x] T009 [P] [US1] Web endpoint tests in `tests/NatsManager.Web.Tests/Endpoints/CoreNatsEndpointTests.cs`: `GetSubjects_WhenMonitoringAvailable_Returns200WithSubjectList`, `GetSubjects_WhenMonitoringUnavailable_Returns200WithEmptyListAndUnavailableHeader`
- [x] T010 [P] [US1] Component test `src/NatsManager.Frontend/src/features/corenats/components/SubjectBrowser.test.tsx`: renders subject table with data; filter reduces rows; shows "unavailable" placeholder when `X-Subjects-Source: unavailable`; shows "no subjects" empty state when zero results

### Implementation for User Story 1

- [x] T011 [US1] Implement `ListSubjectsAsync` in `src/NatsManager.Infrastructure/Nats/CoreNatsAdapter.cs`: inject `IOptions<CoreNatsMonitoringOptions>` and `IHttpClientFactory`; extract host from the configured NATS URL; `GET http://{host}:{DefaultPort}/subsz?subs=1`; parse `subslist` array grouping by subject; return `ListSubjectsResult`; on any exception log warning and return empty result with monitoring unavailable (depends on T003, T004)
- [x] T012 [US1] Introduce `ListSubjectsResult` wrapper in `src/NatsManager.Application/Modules/CoreNats/Models/CoreNatsModels.cs` with `IReadOnlyList<NatsSubjectInfo> Subjects` and `bool IsMonitoringAvailable`; update `GetSubjectsQuery` handler in `src/NatsManager.Application/Modules/CoreNats/Queries/CoreNatsQueries.cs` to return this wrapper (depends on T001)
- [x] T013 [US1] Update `GetSubjects` in `src/NatsManager.Web/Endpoints/CoreNatsEndpoints.cs` to add `X-Subjects-Source` response header (`monitoring` or `unavailable`) based on `ListSubjectsResult.IsMonitoringAvailable` (depends on T012)
- [x] T014 [P] [US1] Add `useSubjects` hook to `src/NatsManager.Frontend/src/features/corenats/hooks/useCoreNats.ts`: query key `['core-nats-subjects', environmentId]`; `refetchInterval: 15000`; reads `X-Subjects-Source` response header to derive `isMonitoringAvailable` (depends on T006)
- [x] T015 [US1] Create `src/NatsManager.Frontend/src/features/corenats/components/SubjectBrowser.tsx`: `TextInput` filter (client-side, 300ms debounce); `Table` with Subject/Subscriptions columns; `LoadingState` while loading; "No subjects match your filter" empty state; "Subject discovery unavailable — monitoring endpoint not reachable" `Alert` when `isMonitoringAvailable === false`; "No active subscriptions found" empty state when monitoring available but zero subjects (depends on T014)
- [x] T016 [US1] Integrate `<SubjectBrowser environmentId={selectedEnvironmentId} />` into `src/NatsManager.Frontend/src/features/corenats/CoreNatsPage.tsx` below the server info `SimpleGrid` (depends on T015)

**Checkpoint**: User Story 1 complete — subject browser visible, filter working, graceful monitoring unavailability handled

---

## Phase 3: User Story 2 — Publish Messages with Full Control (Priority: P2)

**Goal**: `POST /publish` accepts `payloadFormat`, `headers`, and `replyTo`; the UI publish form validates JSON inline, supports dynamic header rows, and shows success/error notifications without clearing the form.

**Independent Test**: Open the publish dialog, select JSON format and type invalid JSON — button should be disabled with inline error. Add a header row with empty key — validation error should show. Fill subject + valid JSON + two headers + reply-to, publish — success notification shown and form fields remain. Publish a second message without reopening the dialog.

### Tests for User Story 2

- [x] T017 [P] [US2] Unit tests in `tests/NatsManager.Application.Tests/Modules/CoreNats/CoreNatsQueryCommandTests.cs`: `PublishCommand_WithHeaders_PassesHeadersToAdapter`; `PublishCommand_WithJsonFormat_ValidJson_Succeeds`; `PublishCommand_WithJsonFormat_InvalidJson_FailsValidation`; `PublishCommand_WithHexBytesFormat_InvalidHex_FailsValidation`; `PublishCommand_WithEmptyHeaderKey_FailsValidation`; `PublishCommand_WithReplyTo_PassesReplyToToAdapter`
- [x] T018 [P] [US2] Web endpoint tests in `tests/NatsManager.Web.Tests/Endpoints/CoreNatsEndpointTests.cs`: `PublishMessage_WithHeadersAndReplyTo_Returns200`; `PublishMessage_WithInvalidJsonFormat_Returns422`; `PublishMessage_WithInvalidHexFormat_Returns422`
- [x] T019 [P] [US2] Integration test in `tests/NatsManager.Integration.Tests/Nats/CoreNatsAdapterTests.cs`: `PublishAsync_WithHeaders_ShouldNotThrow`; `PublishAsync_WithReplyTo_ShouldNotThrow`
- [x] T020 [P] [US2] Component test `src/NatsManager.Frontend/src/features/corenats/components/PublishMessageForm.test.tsx`: JSON format + invalid JSON disables submit; empty header key shows validation error; success notification shown after mutation success; error notification shown after mutation failure with fields preserved; double-click publish only fires once

### Implementation for User Story 2

- [x] T021 [US2] Update `PublishMessageCommand` in `src/NatsManager.Application/Modules/CoreNats/Commands/CoreNatsCommands.cs`: add `PayloadFormat PayloadFormat`, `IReadOnlyDictionary<string, string> Headers`, `string? ReplyTo`; update `PublishMessageCommandValidator` with header-key-not-empty rule, JSON validation (`JsonDocument.Parse`), hex validation rule; update handler to encode payload per format and pass headers/replyTo to adapter (depends on T001, T002)
- [x] T022 [US2] Update `CoreNatsAdapter.PublishAsync` in `src/NatsManager.Infrastructure/Nats/CoreNatsAdapter.cs`: build `NatsHeaders` from dictionary when non-empty; pass `replyTo` to `connection.PublishAsync` (depends on T002)
- [x] T023 [US2] Update `PublishMessageBody` and `PublishMessage` handler in `src/NatsManager.Web/Endpoints/CoreNatsEndpoints.cs`: expand record with `PayloadFormat`, `Dictionary<string, string>? Headers`, `string? ReplyTo`; map all fields to command (depends on T002)
- [x] T024 [P] [US2] Update `usePublishMessage` in `src/NatsManager.Frontend/src/features/corenats/hooks/useCoreNats.ts` to accept `PublishRequest` (instead of `{subject, payload}`) (depends on T006)
- [x] T025 [US2] Create `src/NatsManager.Frontend/src/features/corenats/components/PublishMessageForm.tsx`: `SegmentedControl` for `PayloadFormat`; `Textarea` for payload with inline JSON and hex validation errors; dynamic header rows (key + value `TextInput` + delete button) with "Add Header" button; `TextInput` for reply-to; `Button` disabled while `isPending` or validation fails; green `Notification` on success (form not cleared); red `Notification` on error (fields preserved) (depends on T024)
- [x] T026 [US2] Replace inline publish modal in `src/NatsManager.Frontend/src/features/corenats/CoreNatsPage.tsx` with `<PublishMessageForm>` inside the existing `<Modal>` (depends on T025)

**Checkpoint**: User Story 2 complete — expanded publish form working with headers, format selection, reply-to, and proper success/error UX

---

## Phase 4: User Story 3 — View Live Messages on a Subject (Priority: P3)

**Goal**: `GET /stream?subject=` SSE endpoint bridges a NATS subscription to the browser; `LiveMessageViewer` component supports subscribe/unsubscribe, pause/resume, configurable cap, and expandable message rows with `PayloadViewer`.

**Independent Test**: Subscribe to `test.>`, publish a message via the publish form — message appears in the viewer within 2 seconds with correct subject, payload, and headers. Click Pause, publish another message — pending counter increments. Click Resume — pending message flushes to the list. Navigate away — verify in server logs that the NATS subscription is closed.

### Tests for User Story 3

- [x] T027 [P] [US3] Unit tests in `tests/NatsManager.Application.Tests/Modules/CoreNats/CoreNatsQueryCommandTests.cs` (not required; no application-layer query was added for subscribe validation)
- [x] T028 [P] [US3] Web endpoint tests in `tests/NatsManager.Web.Tests/Endpoints/CoreNatsEndpointTests.cs`: `StreamEndpoint_WithEmptySubject_Returns400`; `StreamEndpoint_WithSubjectContainingSpaces_Returns400`; `StreamEndpoint_WithValidSubject_ReturnsTextEventStream`
- [x] T029 [P] [US3] Integration test in `tests/NatsManager.Integration.Tests/Nats/CoreNatsAdapterTests.cs`: `SubscribeAsync_WhenMessagePublished_YieldsMessage`; cancellation stops enumeration
- [x] T030 [P] [US3] Hook tests in `src/NatsManager.Frontend/src/features/corenats/hooks/useCoreNats.test.ts`: `subscribe_OpensEventSource`; `unsubscribe_ClosesEventSource`; `pause_StopsDisplayUpdates_IncrementsPendingCount`; `resume_FlushesBufferedMessages`; `cap_TrimsOldestMessages`; `unmount_ClosesEventSource`
- [x] T031 [P] [US3] Component test `src/NatsManager.Frontend/src/features/corenats/components/LiveMessageViewer.test.tsx`: Subscribe button triggers SSE; message appears in table; Pause/Resume flow works; clear empties list; invalid subject pattern shows inline warning

### Implementation for User Story 3

- [x] T032 [US3] Implement `SubscribeAsync` in `src/NatsManager.Infrastructure/Nats/CoreNatsAdapter.cs`: get connection; call `connection.SubscribeAsync<byte[]>(subject, cancellationToken: cancellationToken)`; for each `NatsMsg<byte[]>` yield a `NatsLiveMessage` with subject, `DateTimeOffset.UtcNow`, Base64 payload, size, flattened headers, replyTo, and `IsBinary` flag (depends on T002)
- [x] T033 [US3] Add `GET /stream` endpoint in `src/NatsManager.Web/Endpoints/CoreNatsEndpoints.cs`: validate `subject` query param (non-empty, no spaces → `400`); set `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`; iterate `SubscribeAsync` writing `event: message\ndata: {json}\n\n` per message, flushing after each; loop exits when `HttpContext.RequestAborted` fires (depends on T002, T032)
- [x] T034 [US3] Add `useLiveMessages` hook in `src/NatsManager.Frontend/src/features/corenats/hooks/useCoreNats.ts`: manage `EventSource` via `useEffect`; expose `messages`, `isConnected`, `isPaused`, `pendingCount`, `cap`, `setCap`, `subscribe`, `unsubscribe`, `pause`, `resume`, `clear`; on each SSE `message` event route to display list or `pendingBuffer` ref based on `isPaused`; flush buffer on `resume()`; trim to cap on new message; close `EventSource` on unmount (depends on T006)
- [x] T035 [US3] Create `src/NatsManager.Frontend/src/features/corenats/components/LiveMessageViewer.tsx`: subject pattern `TextInput` + Subscribe/Unsubscribe buttons + connection status `Badge`; `NumberInput` for cap (100–500); Pause/Resume `Button` + pending count `Badge`; Clear button; `Table` (most-recent-first) with Subject/Time/Payload Preview/Headers count columns; expandable rows showing full payload via `<PayloadViewer>` and header key/value list; empty state; inline warning for subject patterns containing spaces (depends on T034)
- [x] T036 [US3] Integrate `<LiveMessageViewer environmentId={selectedEnvironmentId} />` into `src/NatsManager.Frontend/src/features/corenats/CoreNatsPage.tsx` as a new section below the subject browser (depends on T035)

**Checkpoint**: User Story 3 complete — live message viewer works end-to-end with pause/resume, cap enforcement, expandable rows, and automatic subscription teardown

---

## Phase 5: E2E Tests (All Stories)

**Purpose**: Playwright E2E tests covering acceptance scenarios for all three user stories against a real running stack.

- [x] T037 [P] [US1] Add E2E test `GetSubjectsViaApi_ReturnsSubjectsWhenMonitoringAvailable` to `tests/NatsManager.E2E.Tests/Tests/CoreNatsTests.cs`: start a NATS subscriber, call `GET /subjects`, assert at least one entry returned
- [x] T038 [P] [US1] Add E2E test `CoreNatsPage_ShowsSubjectTable_WhenSubscriberActive`: navigate to Core NATS, start NATS subscriber, wait for subject table row to appear within 20s auto-refresh window
- [x] T039 [P] [US1] Add E2E test `SubjectFilter_ReducesVisibleRows`: subject table visible, type filter string, assert fewer rows visible
- [x] T040 [P] [US2] Add E2E test `CanPublishMessage_WithHeadersAndReplyTo` to `tests/NatsManager.E2E.Tests/Tests/CoreNatsTests.cs`: open publish dialog, select JSON format, enter JSON payload, add header, add reply-to, click Publish, assert success notification
- [x] T041 [P] [US2] Add E2E test `CanPublishMessage_WithJsonAndHeaders_ViaApi`: HTTP API test publishing with `payloadFormat: Json`, `headers`, `replyTo`; assert `200` and `published: true`
- [x] T042 [P] [US3] Add E2E test `CanPublishMessage_ViaApi_WithHexBytesFormat`: HTTP API test with `payloadFormat: HexBytes` and valid hex payload; assert `200`
- [x] T043 [P] [US3] Add E2E test `StreamEndpoint_Returns400_ForEmptySubject`: HTTP API test calling `GET /stream` with no subject query param; assert `400`
- [x] T044 [P] [US3] Add E2E test `LiveViewer_ReceivesPublishedMessage` (Playwright UI test): subscribe to `test.e2e.>` in viewer, publish message via publish dialog, assert message row appears in viewer table within 5s

---

## Phase 6: Polish & Final Validation

**Purpose**: Lint, build, and full test validation.

- [ ] T045 Run full backend test suite and confirm all tests pass: `dotnet test` from repository root
- [ ] T046 Run frontend tests and confirm all tests pass: `cd src/NatsManager.Frontend && npm test`
- [ ] T047 [P] Verify backend lint: `dotnet format --verify-no-changes` from repository root
- [ ] T048 [P] Verify frontend lint: `cd src/NatsManager.Frontend && npm run lint`
- [ ] T049 Run E2E tests (requires Aspire stack): `dotnet test tests/NatsManager.E2E.Tests`
- [ ] T050 [P] Verify quickstart steps in `specs/002-core-nats-subjects-messaging/quickstart.md` are accurate and all verification steps pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Foundation)**: No dependencies — can start immediately
- **Phase 2 (US1)**: Depends on Phase 1 completion — T007–T016 can begin after T001–T006 are done
- **Phase 3 (US2)**: Depends on Phase 1 completion — T017–T026 can begin after T001–T006 are done
- **Phase 4 (US3)**: Depends on Phase 1 completion — T027–T036 can begin after T001–T006 are done
- **Phase 5 (E2E)**: Depends on Phase 2, 3, and 4 completion
- **Phase 6 (Polish)**: Depends on Phase 5 completion

### User Story Dependencies

- **US1** can start immediately after Phase 1
- **US2** can start immediately after Phase 1 (independent of US1)
- **US3** can start immediately after Phase 1 (independent of US1, US2)
- All three stories can be developed in parallel by different developers once Phase 1 is complete

### Within Each User Story

- Tests (marked [P]) should be written before implementation where possible — verify they fail before implementing
- Backend changes (adapter → command → endpoint) must be done in that order within each story
- Frontend changes (types → hook → component → page integration) follow the same dependency order
- Each story's implementation tasks depend on T001–T006 (Phase 1) completing first

### Parallel Opportunities

All `[P]`-marked tasks within the same phase can run simultaneously.

Once Phase 1 is complete, all three user stories can be built in parallel:
- **Developer A**: US1 (T007–T016)
- **Developer B**: US2 (T017–T026)
- **Developer C**: US3 (T027–T036)

Within US2 specifically, backend (T017–T023) and frontend (T024–T026) can proceed in parallel once the shared types (T006) are done.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (T001–T006) — ~2 hours
2. Complete Phase 2 US1 (T007–T016) — subject browser working
3. **STOP and VALIDATE**: navigate to Core NATS with an active NATS subscriber, confirm subject table, filter, and graceful degradation
4. Demo or merge as incremental improvement

### Incremental Delivery

1. Phase 1 → Foundation ready
2. Phase 2 → Subject browser live (P1 delivered)
3. Phase 3 → Expanded publish (P2 delivered)
4. Phase 4 → Live message viewer (P3 delivered)
5. Phase 5 + 6 → E2E coverage + validation

### Parallel Team Strategy

With three developers after Phase 1 completes:
- Dev A owns US1 (T007–T016): subject browser
- Dev B owns US2 (T017–T026): expanded publish
- Dev C owns US3 (T027–T036): live message viewer

Stories integrate into `CoreNatsPage.tsx` at T016, T026, and T036 — merge order: US1 first (page rework), then US2 (replaces modal content), then US3 (new section added).

---

## Notes

- `[P]` tasks = different files, no dependencies within the phase
- `[US1/US2/US3]` label maps each task to its user story for traceability and independent delivery
- Write tests first, verify they fail, then implement
- Commit after each task or logical group
- Stop at each `**Checkpoint**` to validate independently before proceeding
- The `X-Subjects-Source` response header (T013) requires custom Axios response interceptor or per-query header reading in T014 — use `axios.get(..., { observe: 'response' })` pattern consistent with existing API client
- SSE `EventSource` does not support custom request headers, so session auth via cookie is used (already configured globally) — no special auth setup needed for T033/T034

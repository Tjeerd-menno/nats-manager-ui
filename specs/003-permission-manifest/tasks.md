# Tasks: Application Permission Exposure

**Input**: Design documents from `/specs/003-permission-manifest/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Required by the project constitution. Test tasks appear before implementation tasks in each user story.

**Organization**: Tasks are grouped by user story so each story can be implemented, tested, and delivered independently after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files or does not depend on incomplete tasks
- **[Story]**: Maps the task to a user story from spec.md
- Every task includes an exact target path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature file layout and test entry points without implementing behavior.

- [X] T001 Create the permissions module directory structure under `src/NatsManager.Application/Modules/Permissions/`
- [X] T002 [P] Create the web endpoint file shell in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T003 [P] Create application test directory shells under `tests/NatsManager.Application.Tests/Modules/Permissions/`
- [X] T004 [P] Create the endpoint test file shell in `tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared contracts, options, and service surfaces that all user stories depend on.

**Critical**: No user story work should begin until this phase is complete.

- [X] T005 [P] Define `PermissionManifest`, `ApplicationMetadata`, `PermissionDefinition`, publication status enums, and retrieval result records in `src/NatsManager.Application/Modules/Permissions/Models/PermissionManifestModels.cs`
- [X] T006 [P] Define `GetPermissionManifestQuery` and `GetPermissionManifestQueryHandler` signatures in `src/NatsManager.Application/Modules/Permissions/Queries/GetPermissionManifestQuery.cs`
- [X] T007 [P] Define `PermissionManifestOptions` and `PermissionManifestExposureMode` with public/restricted configuration properties in `src/NatsManager.Web/Configuration/PermissionManifestOptions.cs`
- [X] T008 Define the validator service contract and result shape in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestValidator.cs`
- [X] T009 Define the registry service contract for active application-owned permissions in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestRegistry.cs`
- [X] T010 Define the publisher service contract and in-memory publication state fields in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestPublisher.cs`

**Checkpoint**: Foundation ready. User story implementation can now begin in priority order or in parallel across separate contributors.

---

## Phase 3: User Story 1 - Discover Application Permissions (Priority: P1) MVP

**Goal**: `GET /.well-known/permissions` returns a successful JSON manifest with application metadata, all active owned permissions, and human-readable descriptions.

**Independent Test**: Request `/.well-known/permissions` in public mode and verify HTTP 200, `application.id`, `application.name`, a permissions array, unique permission names, non-empty descriptions, and `X-Permission-Manifest-Source: current`.

### Tests for User Story 1

- [X] T011 [P] [US1] Add validator tests for a valid manifest and missing required application/permission fields in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs`
- [X] T012 [P] [US1] Add publisher tests for successful validation, publication, and current-source retrieval in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestPublisherTests.cs`
- [X] T013 [P] [US1] Add public-mode endpoint contract test for HTTP 200, JSON content type, manifest shape, and current-source header in `tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Implement required-field validation for application metadata, permission names, and permission descriptions in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestValidator.cs`
- [X] T015 [US1] Implement the initial active permission manifest source for NATS Manager in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestRegistry.cs`
- [X] T016 [US1] Implement `GetPermissionManifestQueryHandler` to retrieve the published manifest result from the publisher in `src/NatsManager.Application/Modules/Permissions/Queries/GetPermissionManifestQuery.cs`
- [X] T017 [US1] Implement successful publication and current-source retrieval behavior in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestPublisher.cs`
- [X] T018 [US1] Implement the public happy-path Minimal API handler for `/.well-known/permissions` in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T019 [US1] Register permission manifest services, options binding, and endpoint mapping in `src/NatsManager.Web/Program.cs`
- [X] T020 [US1] Run the US1 focused tests using commands documented in `specs/003-permission-manifest/quickstart.md`

**Checkpoint**: User Story 1 is functional and independently testable as the MVP.

---

## Phase 4: User Story 2 - Publish Permission Metadata for Administration (Priority: P2)

**Goal**: The manifest includes administration-friendly metadata such as category values and optional application version without changing the stable required payload shape.

**Independent Test**: Retrieve a manifest containing categorized permissions and application version metadata, then verify categories are present where defined and required fields remain unchanged.

### Tests for User Story 2

- [X] T021 [P] [US2] Add validator tests for optional non-empty category values and optional application version handling in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs`
- [X] T022 [P] [US2] Add endpoint contract test for categorized permissions and application version serialization in `tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs`
- [X] T023 [P] [US2] Add options binding tests for application id, name, version, and exposure mode in `tests/NatsManager.Web.Tests/Configuration/PermissionManifestOptionsTests.cs`

### Implementation for User Story 2

- [X] T024 [US2] Implement optional category and application version serialization behavior in `src/NatsManager.Application/Modules/Permissions/Models/PermissionManifestModels.cs`
- [X] T025 [US2] Populate category metadata for published permissions in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestRegistry.cs`
- [X] T026 [US2] Resolve application id, name, and optional version from configured options or assembly metadata before publication in `src/NatsManager.Web/Program.cs`
- [X] T027 [US2] Apply `PermissionManifestOptions` validation for application identity fields in `src/NatsManager.Web/Configuration/PermissionManifestOptions.cs`
- [X] T028 [US2] Ensure endpoint responses preserve the stable required payload shape while including optional metadata in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T029 [US2] Run the US2 focused tests using commands documented in `specs/003-permission-manifest/quickstart.md`

**Checkpoint**: User Stories 1 and 2 both work independently and the endpoint is useful for administration grouping.

---

## Phase 5: User Story 3 - Reflect Permission Changes (Priority: P3)

**Goal**: Successful manifest updates appear on the next retrieval, invalid current manifests fall back to the last valid manifest when available, restricted deployments enforce access controls, and inactive permissions are never published.

**Independent Test**: Publish an initial valid manifest, publish changed manifests with additions/removals/renames, verify the next retrieval reflects the latest successful publication, then verify invalid current manifests return last-valid or 503 according to prior valid state.

### Tests for User Story 3

- [X] T030 [P] [US3] Add publisher tests for add/remove/rename update publication and next-retrieval freshness in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestPublisherTests.cs`
- [X] T031 [US3] Add publisher tests for invalid-current fallback and invalid-no-fallback states in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestPublisherTests.cs`
- [X] T032 [P] [US3] Add validator tests for duplicate permission names and inactive permission exclusion in `tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs`
- [X] T033 [P] [US3] Add endpoint tests for last-valid `200` responses and no-valid-manifest `503` problem responses in `tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs`
- [X] T034 [US3] Add endpoint tests for restricted mode missing key, invalid key, and valid key responses in `tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs`

### Implementation for User Story 3

- [X] T035 [US3] Implement duplicate permission name rejection and trimmed field validation in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestValidator.cs`
- [X] T036 [US3] Implement active-only registry entries so deprecated and inactive permissions are excluded from published manifests in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestRegistry.cs`
- [X] T037 [US3] Implement publication state transitions for `Valid`, `InvalidWithFallback`, and `InvalidNoFallback` in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestPublisher.cs`
- [X] T038 [US3] Implement next-retrieval freshness after successful publish operations in `src/NatsManager.Application/Modules/Permissions/Services/PermissionManifestPublisher.cs`
- [X] T039 [US3] Implement restricted-mode service key comparison and unauthorized problem responses in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T040 [US3] Implement `503` problem responses and `X-Permission-Manifest-Source` header selection for current versus last-valid retrievals in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T041 [US3] Enforce explicit exposure mode and restricted key configuration validation in `src/NatsManager.Web/Configuration/PermissionManifestOptions.cs`
- [X] T042 [US3] Emit structured logs for manifest retrieval results and validation failures in `src/NatsManager.Web/Endpoints/PermissionManifestEndpoints.cs`
- [X] T043 [US3] Wire startup publication validation so the first retrieval uses validated state in `src/NatsManager.Web/Program.cs`
- [X] T044 [US3] Run the US3 focused tests using commands documented in `specs/003-permission-manifest/quickstart.md`

**Checkpoint**: All user stories are independently functional and the endpoint handles update, fallback, and restricted-access scenarios.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, cleanup, and documentation alignment across the feature.

- [X] T045 [P] Update manual verification notes for final option names and endpoint behavior in `specs/003-permission-manifest/quickstart.md`
- [X] T046 [P] Review the API contract for any implementation-aligned header or problem-details wording updates in `specs/003-permission-manifest/contracts/api-contracts.md`
- [X] T047 Verify no frontend changes are required or accidentally introduced under `src/NatsManager.Frontend/`
- [X] T048 Run full backend test verification from `specs/003-permission-manifest/quickstart.md`
- [X] T049 Run formatting verification from `specs/003-permission-manifest/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion; blocks all user stories.
- **User Stories (Phases 3-5)**: Depend on Foundational completion.
- **Polish (Phase 6)**: Depends on completion of the desired user stories.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundational; no dependency on other user stories; MVP scope.
- **User Story 2 (P2)**: Starts after Foundational; can build on the same manifest model but remains independently testable through metadata assertions.
- **User Story 3 (P3)**: Starts after Foundational; can be implemented after US1 for simplest sequencing because fallback behavior relies on the same publisher contract, but tests isolate update and access-policy behavior.

### Within Each User Story

- Write story tests first and verify they fail before implementation.
- Implement application validation and publication behavior before endpoint response behavior.
- Register services and options before running web endpoint tests.
- Complete each story checkpoint before moving to the next priority when working sequentially.

---

## Parallel Opportunities

- T002, T003, and T004 can run in parallel after T001 because they touch different files.
- T005, T006, and T007 can run in parallel during Foundational work.
- T011, T012, and T013 can run in parallel for US1 test-first development.
- T021, T022, and T023 can run in parallel for US2 test-first development.
- T030, T032, and T033 can be split across application and web test files for US3 while T031 and T034 sequence behind same-file test edits.
- T045 and T046 can run in parallel during polish because they update separate documentation files.

---

## Parallel Example: User Story 1

```text
Task: "T011 [P] [US1] Add validator tests for a valid manifest and missing required application/permission fields in tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs"
Task: "T012 [P] [US1] Add publisher tests for successful validation, publication, and current-source retrieval in tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestPublisherTests.cs"
Task: "T013 [P] [US1] Add public-mode endpoint contract test for HTTP 200, JSON content type, manifest shape, and current-source header in tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "T021 [P] [US2] Add validator tests for optional non-empty category values and optional application version handling in tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs"
Task: "T022 [P] [US2] Add endpoint contract test for categorized permissions and application version serialization in tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs"
Task: "T023 [P] [US2] Add options binding tests for application id, name, version, and exposure mode in tests/NatsManager.Web.Tests/Configuration/PermissionManifestOptionsTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T030 [P] [US3] Add publisher tests for add/remove/rename update publication and next-retrieval freshness in tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestPublisherTests.cs"
Task: "T032 [P] [US3] Add validator tests for duplicate permission names and inactive permission exclusion in tests/NatsManager.Application.Tests/Modules/Permissions/PermissionManifestValidatorTests.cs"
Task: "T033 [P] [US3] Add endpoint tests for last-valid 200 responses and no-valid-manifest 503 problem responses in tests/NatsManager.Web.Tests/Endpoints/PermissionManifestEndpointTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational contracts and service surfaces.
3. Complete Phase 3: User Story 1.
4. Stop and validate US1 with the focused application and web tests from `specs/003-permission-manifest/quickstart.md`.

### Incremental Delivery

1. Deliver US1 to expose the core well-known permission manifest.
2. Deliver US2 to enrich the manifest for administration grouping and version-aware review.
3. Deliver US3 to handle manifest lifecycle updates, fallback behavior, and restricted deployments.
4. Run full backend verification and formatting before completion.

### Parallel Team Strategy

1. Complete Setup and Foundational work together.
2. Split tests by boundary: application validator/publisher tests and web endpoint contract tests.
3. Implement application-layer behavior before web-layer response behavior for each story.
4. Integrate through `Program.cs` only after the relevant service behavior is covered by tests.

---

## Notes

- Do not add frontend implementation for this feature unless the specification changes.
- Keep the endpoint at root path `/.well-known/permissions`; do not place it under `/api`.
- Successful responses serialize only active permissions and must never return unvalidated manifest data.
- Restricted mode must be explicit per deployment and must not rely on browser session authentication.

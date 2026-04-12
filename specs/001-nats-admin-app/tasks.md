# Tasks: NATS Admin Application

**Input**: Design documents from `/specs/001-nats-admin-app/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Tests**: Included — spec requires xUnit (backend) + Vitest (frontend) with 80% coverage target, contract tests at API boundary.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization — .NET solution, Aspire orchestration, React app, tooling configuration

- [x] T001 Create .NET solution file and project structure: `NatsManager.sln` with projects `src/NatsManager.Domain/`, `src/NatsManager.Application/`, `src/NatsManager.Infrastructure/`, `src/NatsManager.Web/`, `src/NatsManager.AppHost/`, `src/NatsManager.ServiceDefaults/`, `tests/NatsManager.Domain.Tests/`, `tests/NatsManager.Application.Tests/`, `tests/NatsManager.Infrastructure.Tests/`, `tests/NatsManager.Web.Tests/`
- [x] T002 Configure `src/NatsManager.AppHost/NatsManager.AppHost.csproj` with `Aspire.Hosting.Nats` and `Aspire.Hosting.JavaScript` packages; implement `src/NatsManager.AppHost/Program.cs` with `AddNats("nats").WithArgs("-js")`, `AddProject<Projects.NatsManager_Web>("backend").WithReference(nats)`, and `AddViteApp("frontend", "../NatsManager.Frontend").WithReference(backend)`
- [x] T003 [P] Configure `src/NatsManager.ServiceDefaults/NatsManager.ServiceDefaults.csproj` and implement `src/NatsManager.ServiceDefaults/Extensions.cs` with `AddServiceDefaults()` extension method: OpenTelemetry (tracing, metrics, logging), health check endpoints (`/health`, `/alive`), HTTP resilience, service discovery
- [x] T004 [P] Initialize React app in `src/NatsManager.Frontend/` with Vite: `package.json`, `tsconfig.json` (strict mode), `vite.config.ts` (with `@vitejs/plugin-react`, `/api` proxy), `vitest.config.ts` (jsdom, colocated test discovery `src/**/*.test.{ts,tsx}`, `@vitest/coverage-v8`)
- [x] T005 [P] Configure `src/NatsManager.Web/NatsManager.Web.csproj` with dependencies: `Aspire.NATS.Net`, `Serilog`, `FluentValidation`, project references to Domain, Application, Infrastructure, ServiceDefaults; create minimal `src/NatsManager.Web/Program.cs` with `AddServiceDefaults()`, Serilog, and health endpoints
- [x] T006 [P] Configure `src/NatsManager.Domain/NatsManager.Domain.csproj` (no external dependencies); create module folders under `src/NatsManager.Domain/Modules/`: Environments, JetStream, KeyValue, ObjectStore, Services, CoreNats, Auth, Audit, Shared
- [x] T007 [P] Configure `src/NatsManager.Application/NatsManager.Application.csproj` with MediatR, FluentValidation; create module folders under `src/NatsManager.Application/Modules/` mirroring domain modules
- [x] T008 [P] Configure `src/NatsManager.Infrastructure/NatsManager.Infrastructure.csproj` with EF Core 10 (SQLite provider), NATS.Net v2; create folders `src/NatsManager.Infrastructure/Persistence/`, `src/NatsManager.Infrastructure/Nats/`, `src/NatsManager.Infrastructure/Auth/`
- [x] T009 [P] Configure backend test projects with xUnit, FluentAssertions: `tests/NatsManager.Domain.Tests/`, `tests/NatsManager.Application.Tests/`, `tests/NatsManager.Infrastructure.Tests/`, `tests/NatsManager.Web.Tests/`
- [x] T010 [P] Configure ESLint + Prettier for frontend in `src/NatsManager.Frontend/.eslintrc.cjs` and `src/NatsManager.Frontend/.prettierrc`; configure `dotnet format` analyzers for backend via `.editorconfig` and `Directory.Build.props`
- [x] T011 [P] Install frontend dependencies in `src/NatsManager.Frontend/package.json`: React 19, React DOM, React Router, Mantine 7 (`@mantine/core`, `@mantine/hooks`, `@mantine/form`, `@mantine/notifications`), `@tanstack/react-query`, `@tanstack/react-virtual`, Recharts, Axios; dev deps: Vitest, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom`, `@vitest/coverage-v8`, MSW
- [x] T012 [P] Create production Dockerfile in `src/NatsManager.Web/Dockerfile`: multi-stage build (node:lts-alpine for Vite frontend build → dotnet sdk:10.0 for backend build → dotnet aspnet:10.0-alpine runtime with static assets)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can begin

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T013 Implement domain enums in `src/NatsManager.Domain/Modules/Shared/Enums.cs`: ConnectionStatus, CredentialType, ActionType, ResourceType, Outcome, AuditSource, KeyOperation
- [x] T014 Implement Environment aggregate root in `src/NatsManager.Domain/Modules/Environments/Environment.cs` per data-model.md (Id, Name, Description, ServerUrl, CredentialType, CredentialReference, IsEnabled, IsProduction, ConnectionStatus, LastSuccessfulContact, CreatedAt, UpdatedAt)
- [x] T015 [P] Implement User aggregate root in `src/NatsManager.Domain/Modules/Auth/User.cs` per data-model.md (Id, Username, DisplayName, PasswordHash, IsActive, CreatedAt, LastLoginAt)
- [x] T016 [P] Implement Role entity in `src/NatsManager.Domain/Modules/Auth/Role.cs` and UserRoleAssignment in `src/NatsManager.Domain/Modules/Auth/UserRoleAssignment.cs` per data-model.md
- [x] T017 [P] Implement AuditEvent aggregate root in `src/NatsManager.Domain/Modules/Audit/AuditEvent.cs` per data-model.md (immutable, append-only invariant)
- [x] T018 [P] Implement Bookmark entity in `src/NatsManager.Domain/Modules/Shared/Bookmark.cs` and UserPreference entity in `src/NatsManager.Domain/Modules/Shared/UserPreference.cs` per data-model.md
- [x] T019 Implement EF Core DbContext in `src/NatsManager.Infrastructure/Persistence/AppDbContext.cs` with entity configurations for Environment, User, Role, UserRoleAssignment, AuditEvent, Bookmark, UserPreference; configure SQLite WAL mode
- [x] T020 Create initial EF Core migration in `src/NatsManager.Infrastructure/Persistence/Migrations/` with seed data: 4 predefined roles (ReadOnly, Operator, Administrator, Auditor) and default admin user
- [x] T021 [P] Implement NATS connection factory in `src/NatsManager.Infrastructure/Nats/NatsConnectionFactory.cs` with `INatsConnectionFactory` port in `src/NatsManager.Application/Modules/Environments/Ports/INatsConnectionFactory.cs`: connection-per-environment, lazy init, health checking, 10s timeout
- [x] T022 [P] Implement credential encryption service in `src/NatsManager.Infrastructure/Auth/CredentialEncryptionService.cs` with `ICredentialEncryptionService` port in `src/NatsManager.Application/Modules/Environments/Ports/ICredentialEncryptionService.cs`
- [x] T023 [P] Implement audit event repository in `src/NatsManager.Infrastructure/Persistence/AuditEventRepository.cs` with `IAuditEventRepository` port in `src/NatsManager.Application/Modules/Audit/Ports/IAuditEventRepository.cs`
- [x] T024 [P] Implement MediatR pipeline behaviors in `src/NatsManager.Application/Behaviors/`: `ValidationBehavior.cs` (FluentValidation), `AuditBehavior.cs` (automatic audit logging for commands)
- [x] T025 [P] Implement authentication middleware: cookie-based session auth in `src/NatsManager.Infrastructure/Auth/SessionAuthHandler.cs`, `src/NatsManager.Web/Middleware/AuthenticationMiddleware.cs`; password hashing with Argon2 in `src/NatsManager.Infrastructure/Auth/PasswordHasher.cs`
- [x] T026 [P] Implement authorization service in `src/NatsManager.Application/Modules/Auth/Services/AuthorizationService.cs` with role matrix (ReadOnly, Operator, Administrator, Auditor) and environment-level production restrictions
- [x] T027 [P] Implement ProblemDetails error handling middleware in `src/NatsManager.Web/Middleware/ErrorHandlingMiddleware.cs` per RFC 9457, with domain-specific error types in `src/NatsManager.Domain/Modules/Shared/Errors/`
- [x] T028 [P] Implement data freshness infrastructure: `DataFreshnessHeader` middleware in `src/NatsManager.Web/Middleware/DataFreshnessMiddleware.cs` that sets `X-Data-Freshness` and `X-Data-Timestamp` headers per api-contracts.md
- [x] T029 Implement pagination and sorting support: `PaginatedQuery<T>` base class in `src/NatsManager.Application/Common/PaginatedQuery.cs`, `PaginatedResult<T>` in `src/NatsManager.Application/Common/PaginatedResult.cs`
- [x] T030 Register all services in DI: configure MediatR, FluentValidation, EF Core, NATS connection factory, auth services in `src/NatsManager.Web/Program.cs`
- [x] T031 [P] Create frontend app shell in `src/NatsManager.Frontend/src/App.tsx` with Mantine Provider, React Router, TanStack Query Provider, route-based code splitting; create `src/NatsManager.Frontend/src/main.tsx` entry point
- [x] T032 [P] Create shared frontend components in `src/NatsManager.Frontend/src/shared/`: `AppLayout.tsx` (sidebar nav, environment badge), `ResourceListView.tsx` (paginated table with sort/filter/virtualization), `ResourceDetailView.tsx` (identity → status → config → relationships → actions), `ConfirmActionDialog.tsx` (destructive action confirmation with resource name + environment), `DataFreshnessIndicator.tsx` (live/recent/stale badge), `EnvironmentBadge.tsx`, `ErrorBoundary.tsx`, `LoadingState.tsx`, `EmptyState.tsx`
- [x] T033 [P] Create API client infrastructure in `src/NatsManager.Frontend/src/api/`: `client.ts` (Axios instance with cookie auth, interceptors), `queryClient.ts` (TanStack Query client config with default stale times), `types.ts` (PaginatedResult, ProblemDetails, DataFreshness types)
- [x] T034 [P] Create auth context and hooks in `src/NatsManager.Frontend/src/features/auth/`: `AuthContext.tsx`, `useAuth.ts`, `LoginPage.tsx`, `ProtectedRoute.tsx`

**Checkpoint**: Foundation ready — all domain entities, persistence, auth, shared UI shell, and API infrastructure in place

---

## Phase 3: User Story 1 — Connect to a NATS environment and view its status (Priority: P1) 🎯 MVP

**Goal**: Operators can register NATS environments, see connection status, select environments, and view data freshness indicators.

**Independent Test**: Register a NATS environment and confirm connection status, environment name, and health indicators are displayed.

### Tests for User Story 1

- [x] T035 [P] [US1] Unit tests for Environment domain entity in `tests/NatsManager.Domain.Tests/Modules/Environments/EnvironmentTests.cs`: creation validation, state transitions, invariants
- [x] T036 [P] [US1] Unit tests for environment commands/queries in `tests/NatsManager.Application.Tests/Modules/Environments/`: RegisterEnvironmentCommandTests.cs, UpdateEnvironmentCommandTests.cs, TestConnectionCommandTests.cs, GetEnvironmentsQueryTests.cs
- [x] T037 [P] [US1] Integration tests for environment NATS adapter in `tests/NatsManager.Infrastructure.Tests/Nats/NatsConnectionFactoryTests.cs`: connection, health check, timeout handling
- [x] T038 [P] [US1] Contract tests for environment API endpoints in `tests/NatsManager.Web.Tests/Endpoints/EnvironmentEndpointTests.cs`: GET/POST/PUT/DELETE /api/environments, POST /api/environments/{id}/test per api-contracts.md §1
- [x] T039 [P] [US1] Frontend component tests in `src/NatsManager.Frontend/src/features/environments/`: `EnvironmentList.test.tsx`, `EnvironmentForm.test.tsx`, `EnvironmentSelector.test.tsx`, `ConnectionStatusBadge.test.tsx`

### Implementation for User Story 1

- [x] T040 [P] [US1] Implement environment commands in `src/NatsManager.Application/Modules/Environments/Commands/`: `RegisterEnvironmentCommand.cs` (with FluentValidation), `UpdateEnvironmentCommand.cs`, `DeleteEnvironmentCommand.cs`, `EnableDisableEnvironmentCommand.cs`, `TestConnectionCommand.cs`
- [x] T041 [P] [US1] Implement environment queries in `src/NatsManager.Application/Modules/Environments/Queries/`: `GetEnvironmentsQuery.cs`, `GetEnvironmentDetailQuery.cs`
- [x] T042 [US1] Implement environment repository in `src/NatsManager.Infrastructure/Persistence/EnvironmentRepository.cs` with `IEnvironmentRepository` port in `src/NatsManager.Application/Modules/Environments/Ports/IEnvironmentRepository.cs`
- [x] T043 [US1] Implement NATS health check adapter in `src/NatsManager.Infrastructure/Nats/NatsHealthChecker.cs` with `INatsHealthChecker` port: connection test, status polling, JetStream availability detection
- [x] T044 [US1] Implement environment API endpoints in `src/NatsManager.Web/Endpoints/EnvironmentEndpoints.cs`: GET/POST/PUT/DELETE /api/environments, GET /api/environments/{id}, POST /api/environments/{id}/test per api-contracts.md §1
- [x] T045 [US1] Implement background health polling service in `src/NatsManager.Web/BackgroundServices/EnvironmentHealthPoller.cs`: periodic connection status refresh (configurable, default 30s), update ConnectionStatus and LastSuccessfulContact
- [x] T046 [P] [US1] Implement environment API hooks in `src/NatsManager.Frontend/src/features/environments/hooks/`: `useEnvironments.ts`, `useEnvironment.ts`, `useRegisterEnvironment.ts`, `useUpdateEnvironment.ts`, `useDeleteEnvironment.ts`, `useTestConnection.ts`
- [x] T047 [P] [US1] Implement environment components in `src/NatsManager.Frontend/src/features/environments/components/`: `EnvironmentList.tsx` (list with status indicators), `EnvironmentForm.tsx` (register/edit modal), `EnvironmentDetail.tsx` (detail view with server info), `ConnectionStatusBadge.tsx` (available/degraded/unavailable), `EnvironmentSelector.tsx` (global environment picker in AppLayout)
- [x] T048 [US1] Implement environment context in `src/NatsManager.Frontend/src/features/environments/EnvironmentContext.tsx`: selected environment state, scoping all downstream views; integrate EnvironmentSelector into AppLayout sidebar
- [x] T049 [US1] Create environment types in `src/NatsManager.Frontend/src/features/environments/types.ts` matching api-contracts.md §1 response shapes

**Checkpoint**: User Story 1 complete — environments can be registered, status is visible, environment context scopes all views

---

## Phase 4: User Story 2 — Browse and inspect JetStream streams and consumers (Priority: P2)

**Goal**: Operators can browse streams with summary info, drill into stream details, and view consumer state with health indicators.

**Independent Test**: Connect to a NATS environment with streams and consumers, browse stream list, open stream detail, verify consumer state.

### Tests for User Story 2

- [x] T050 [P] [US2] Unit tests for JetStream queries in `tests/NatsManager.Application.Tests/Modules/JetStream/`: GetStreamsQueryTests.cs, GetStreamDetailQueryTests.cs, GetConsumersQueryTests.cs, GetConsumerDetailQueryTests.cs
- [x] T051 [P] [US2] Integration tests for NATS JetStream adapter in `tests/NatsManager.Infrastructure.Tests/Nats/JetStreamAdapterTests.cs`: list streams, get stream info, list consumers, get consumer info
- [x] T052 [P] [US2] Contract tests for JetStream read endpoints in `tests/NatsManager.Web.Tests/Endpoints/JetStreamReadEndpointTests.cs`: GET streams, GET stream/{name}, GET consumers, GET consumer/{name} per api-contracts.md §3-4
- [x] T053 [P] [US2] Frontend component tests in `src/NatsManager.Frontend/src/features/jetstream/`: `StreamList.test.tsx`, `StreamDetail.test.tsx`, `ConsumerList.test.tsx`, `ConsumerDetail.test.tsx`, `ConsumerHealthBadge.test.tsx`

### Implementation for User Story 2

- [x] T054 [P] [US2] Implement observed resource DTOs in `src/NatsManager.Application/Modules/JetStream/Models/`: `StreamInfo.cs`, `ConsumerInfo.cs` per data-model.md
- [x] T055 [US2] Implement NATS JetStream read adapter in `src/NatsManager.Infrastructure/Nats/JetStreamAdapter.cs` with `IJetStreamAdapter` port in `src/NatsManager.Application/Modules/JetStream/Ports/IJetStreamAdapter.cs`: list streams, get stream info/state, list consumers, get consumer info/state, derive IsHealthy
- [x] T056 [US2] Implement JetStream read queries in `src/NatsManager.Application/Modules/JetStream/Queries/`: `GetStreamsQuery.cs` (paginated, searchable, sortable), `GetStreamDetailQuery.cs`, `GetConsumersQuery.cs`, `GetConsumerDetailQuery.cs`
- [x] T057 [US2] Implement JetStream read endpoints in `src/NatsManager.Web/Endpoints/JetStreamEndpoints.cs`: GET /api/environments/{envId}/jetstream/streams, GET /api/environments/{envId}/jetstream/streams/{name}, GET consumers per api-contracts.md §3-4
- [x] T058 [P] [US2] Implement JetStream API hooks in `src/NatsManager.Frontend/src/features/jetstream/hooks/`: `useStreams.ts`, `useStream.ts`, `useConsumers.ts`, `useConsumer.ts`
- [x] T059 [P] [US2] Implement JetStream components in `src/NatsManager.Frontend/src/features/jetstream/components/`: `StreamList.tsx` (with ResourceListView, virtualization for 1k+, search/filter), `StreamDetail.tsx` (config, state, limits, consumer list), `ConsumerList.tsx` (with health highlighting), `ConsumerDetail.tsx` (backlog, delivery state, ack info), `ConsumerHealthBadge.tsx`
- [x] T060 [US2] Create JetStream types in `src/NatsManager.Frontend/src/features/jetstream/types.ts` matching api-contracts.md §3-4 response shapes
- [x] T061 [US2] Add JetStream routes to `src/NatsManager.Frontend/src/App.tsx` with lazy loading: `/jetstream/streams`, `/jetstream/streams/:name`

**Checkpoint**: User Story 2 complete — streams and consumers are browsable with health indicators

---

## Phase 5: User Story 3 — Create, update, and delete JetStream resources with safeguards (Priority: P3)

**Goal**: Platform engineers can create/update/delete streams and consumers with confirmation dialogs, role enforcement, and audit logging.

**Independent Test**: Create a stream, modify config, delete it — verify confirmation prompts and audit records at each step.

### Tests for User Story 3

- [x] T062 [P] [US3] Unit tests for JetStream commands in `tests/NatsManager.Application.Tests/Modules/JetStream/`: CreateStreamCommandTests.cs, UpdateStreamCommandTests.cs, DeleteStreamCommandTests.cs, PurgeStreamCommandTests.cs, CreateConsumerCommandTests.cs, UpdateConsumerCommandTests.cs, DeleteConsumerCommandTests.cs (authorization, validation, audit)
- [x] T063 [P] [US3] Contract tests for JetStream write endpoints in `tests/NatsManager.Web.Tests/Endpoints/JetStreamWriteEndpointTests.cs`: POST/PUT/DELETE streams, POST purge, POST/PUT/DELETE consumers, X-Confirm header enforcement per api-contracts.md §3-4
- [x] T064 [P] [US3] Frontend component tests in `src/NatsManager.Frontend/src/features/jetstream/`: `StreamForm.test.tsx`, `ConsumerForm.test.tsx`, `DeleteConfirmation.test.tsx`

### Implementation for User Story 3

- [x] T065 [P] [US3] Implement JetStream write commands in `src/NatsManager.Application/Modules/JetStream/Commands/`: `CreateStreamCommand.cs`, `UpdateStreamCommand.cs`, `DeleteStreamCommand.cs`, `PurgeStreamCommand.cs` (with FluentValidation validators for each)
- [x] T066 [P] [US3] Implement consumer write commands in `src/NatsManager.Application/Modules/JetStream/Commands/`: `CreateConsumerCommand.cs`, `UpdateConsumerCommand.cs`, `DeleteConsumerCommand.cs` (with FluentValidation validators)
- [x] T067 [US3] Implement NATS JetStream write adapter methods in `src/NatsManager.Infrastructure/Nats/JetStreamAdapter.cs`: create/update/delete stream, purge stream, create/update/delete consumer
- [x] T068 [US3] Implement JetStream write endpoints in `src/NatsManager.Web/Endpoints/JetStreamEndpoints.cs`: POST/PUT/DELETE streams, POST purge, POST/PUT/DELETE consumers with X-Confirm header validation per api-contracts.md §3-4
- [x] T069 [P] [US3] Implement JetStream mutation hooks in `src/NatsManager.Frontend/src/features/jetstream/hooks/`: `useCreateStream.ts`, `useUpdateStream.ts`, `useDeleteStream.ts`, `usePurgeStream.ts`, `useCreateConsumer.ts`, `useUpdateConsumer.ts`, `useDeleteConsumer.ts` (with TanStack Query cache invalidation)
- [x] T070 [P] [US3] Implement JetStream form components in `src/NatsManager.Frontend/src/features/jetstream/components/`: `StreamForm.tsx` (create/edit modal with validation), `ConsumerForm.tsx` (create/edit modal), `StreamActions.tsx` (delete/purge buttons with role-based visibility), `ConsumerActions.tsx`
- [x] T071 [US3] Implement message browsing: query `GetStreamMessagesQuery.cs` in `src/NatsManager.Application/Modules/JetStream/Queries/`, adapter method in `src/NatsManager.Infrastructure/Nats/JetStreamAdapter.cs`, endpoint GET /api/environments/{envId}/jetstream/streams/{name}/messages in `src/NatsManager.Web/Endpoints/JetStreamEndpoints.cs`, frontend `MessageBrowser.tsx` in `src/NatsManager.Frontend/src/features/jetstream/components/`, `useStreamMessages.ts` hook

**Checkpoint**: User Story 3 complete — full JetStream CRUD with safeguards

---

## Phase 6: User Story 4 — Inspect and manage Key-Value Store buckets and keys (Priority: P4)

**Goal**: Developers can browse KV buckets, inspect key values/revisions, distinguish key states, and create/update keys with overwrite protection.

**Independent Test**: Browse KV buckets, inspect key values and revisions, create/update a key with overwrite confirmation.

### Tests for User Story 4

- [x] T072 [P] [US4] Unit tests for KV commands/queries in `tests/NatsManager.Application.Tests/Modules/KeyValue/`: GetKvBucketsQueryTests.cs, GetKvKeysQueryTests.cs, GetKvKeyDetailQueryTests.cs, CreateKvBucketCommandTests.cs, PutKvKeyCommandTests.cs, DeleteKvKeyCommandTests.cs (optimistic concurrency)
- [x] T073 [P] [US4] Integration tests for NATS KV adapter in `tests/NatsManager.Infrastructure.Tests/Nats/KvStoreAdapterTests.cs`: list buckets, get/put/delete keys, history, revision conflict
- [x] T074 [P] [US4] Contract tests for KV endpoints in `tests/NatsManager.Web.Tests/Endpoints/KvEndpointTests.cs`: all endpoints per api-contracts.md §5
- [x] T075 [P] [US4] Frontend component tests in `src/NatsManager.Frontend/src/features/kv/`: `KvBucketList.test.tsx`, `KvKeyList.test.tsx`, `KvKeyDetail.test.tsx`, `KvKeyEditor.test.tsx`

### Implementation for User Story 4

- [x] T076 [P] [US4] Implement KV observed DTOs in `src/NatsManager.Application/Modules/KeyValue/Models/`: `KvBucketInfo.cs`, `KvEntry.cs` per data-model.md
- [x] T077 [US4] Implement NATS KV adapter in `src/NatsManager.Infrastructure/Nats/KvStoreAdapter.cs` with `IKvStoreAdapter` port in `src/NatsManager.Application/Modules/KeyValue/Ports/IKvStoreAdapter.cs`: list buckets, get bucket detail, list keys, get key value, get key history, put key (optimistic concurrency via revision), delete key, create bucket, delete bucket
- [x] T078 [US4] Implement KV queries in `src/NatsManager.Application/Modules/KeyValue/Queries/`: `GetKvBucketsQuery.cs`, `GetKvBucketDetailQuery.cs`, `GetKvKeysQuery.cs` (searchable), `GetKvKeyDetailQuery.cs`, `GetKvKeyHistoryQuery.cs`
- [x] T079 [US4] Implement KV commands in `src/NatsManager.Application/Modules/KeyValue/Commands/`: `CreateKvBucketCommand.cs`, `DeleteKvBucketCommand.cs`, `PutKvKeyCommand.cs` (with expectedRevision for optimistic concurrency, 409 Conflict), `DeleteKvKeyCommand.cs`
- [x] T080 [US4] Implement KV API endpoints in `src/NatsManager.Web/Endpoints/KvEndpoints.cs`: all endpoints per api-contracts.md §5
- [x] T081 [P] [US4] Implement KV API hooks in `src/NatsManager.Frontend/src/features/kv/hooks/`: `useKvBuckets.ts`, `useKvBucket.ts`, `useKvKeys.ts`, `useKvKey.ts`, `useKvKeyHistory.ts`, `usePutKvKey.ts`, `useDeleteKvKey.ts`, `useCreateKvBucket.ts`, `useDeleteKvBucket.ts`
- [x] T082 [P] [US4] Implement KV components in `src/NatsManager.Frontend/src/features/kv/components/`: `KvBucketList.tsx` (ResourceListView), `KvBucketDetail.tsx`, `KvKeyList.tsx` (with key state indicators: current/deleted/superseded), `KvKeyDetail.tsx` (value display, revision history, metadata), `KvKeyEditor.tsx` (create/update with overwrite warning on revision conflict), `KvBucketForm.tsx`
- [x] T083 [US4] Create KV types in `src/NatsManager.Frontend/src/features/kv/types.ts` and add KV routes to `src/NatsManager.Frontend/src/App.tsx` with lazy loading

**Checkpoint**: User Story 4 complete — KV buckets and keys browsable with overwrite protection

---

## Phase 7: User Story 5 — View environment dashboard with cross-resource health summary (Priority: P5)

**Goal**: Dashboard summarizes health across Core NATS, JetStream, KV, Object Store, and Services with drill-down navigation.

**Independent Test**: Connect to an environment with resources across capability areas, verify dashboard surfaces accurate health summaries with working drill-down links.

### Tests for User Story 5

- [x] T084 [P] [US5] Unit tests for dashboard query in `tests/NatsManager.Application.Tests/Modules/Dashboard/GetDashboardQueryTests.cs`: aggregation logic, alert detection (consumer backlog >80%, storage >80%, service unavailable)
- [x] T085 [P] [US5] Contract test for dashboard endpoint in `tests/NatsManager.Web.Tests/Endpoints/DashboardEndpointTests.cs`: GET /api/environments/{envId}/monitoring/dashboard per api-contracts.md §8
- [x] T086 [P] [US5] Frontend component tests in `src/NatsManager.Frontend/src/features/dashboard/`: `Dashboard.test.tsx`, `HealthSummaryCard.test.tsx`, `AlertList.test.tsx`

### Implementation for User Story 5

- [x] T087 [US5] Implement dashboard aggregation query in `src/NatsManager.Application/Modules/Dashboard/Queries/GetDashboardQuery.cs`: aggregate health from JetStream adapter (streams, consumers, unhealthy count), KV adapter (buckets, keys), connection status, detect alerts per FR-044 thresholds
- [x] T088 [US5] Implement dashboard endpoint in `src/NatsManager.Web/Endpoints/DashboardEndpoints.cs`: GET /api/environments/{envId}/monitoring/dashboard per api-contracts.md §8
- [x] T089 [P] [US5] Implement dashboard API hook in `src/NatsManager.Frontend/src/features/dashboard/hooks/useDashboard.ts` with auto-refresh (polling interval)
- [x] T090 [P] [US5] Implement dashboard components in `src/NatsManager.Frontend/src/features/dashboard/components/`: `Dashboard.tsx` (layout with summary cards and alert list), `HealthSummaryCard.tsx` (Core NATS, JetStream, KV, Object Store, Services — each with count + status), `AlertList.tsx` (severity-based highlighting with drill-down links), `DegradedEnvironmentBanner.tsx`
- [x] T091 [US5] Create dashboard types in `src/NatsManager.Frontend/src/features/dashboard/types.ts` and add dashboard route (`/dashboard`) as default landing page in `src/NatsManager.Frontend/src/App.tsx`

**Checkpoint**: User Story 5 complete — single-pane-of-glass dashboard operational

---

## Phase 8: User Story 6 — Discover and test NATS Services (Priority: P6)

**Goal**: Developers can discover services, view endpoints/versions/health, and send test requests with side-effect warnings.

**Independent Test**: Discover a service, view metadata, send a test request with response inspection.

### Tests for User Story 6

- [x] T092 [P] [US6] Unit tests for services queries/commands in `tests/NatsManager.Application.Tests/Modules/Services/`: GetServicesQueryTests.cs, GetServiceDetailQueryTests.cs, TestServiceRequestCommandTests.cs
- [x] T093 [P] [US6] Integration tests for NATS service discovery adapter in `tests/NatsManager.Infrastructure.Tests/Nats/ServiceDiscoveryAdapterTests.cs`: `$SRV.INFO`, `$SRV.PING`, `$SRV.STATS`
- [x] T094 [P] [US6] Contract tests for service endpoints in `tests/NatsManager.Web.Tests/Endpoints/ServiceEndpointTests.cs`: per api-contracts.md §7
- [x] T095 [P] [US6] Frontend tests in `src/NatsManager.Frontend/src/features/services/`: `ServiceList.test.tsx`, `ServiceDetail.test.tsx`, `ServiceTestDialog.test.tsx`

### Implementation for User Story 6

- [x] T096 [P] [US6] Implement service observed DTOs in `src/NatsManager.Application/Modules/Services/Models/`: `ServiceInfo.cs`, `ServiceEndpoint.cs` per data-model.md
- [x] T097 [US6] Implement NATS service discovery adapter in `src/NatsManager.Infrastructure/Nats/ServiceDiscoveryAdapter.cs` with `IServiceDiscoveryAdapter` port in `src/NatsManager.Application/Modules/Services/Ports/IServiceDiscoveryAdapter.cs`: discover via `$SRV.INFO`, ping via `$SRV.PING`, stats via `$SRV.STATS`, send test request
- [x] T098 [US6] Implement service queries in `src/NatsManager.Application/Modules/Services/Queries/`: `GetServicesQuery.cs`, `GetServiceDetailQuery.cs`; service command: `TestServiceRequestCommand.cs` (with side-effect warning acknowledgment)
- [x] T099 [US6] Implement service API endpoints in `src/NatsManager.Web/Endpoints/ServiceEndpoints.cs`: GET /api/environments/{envId}/services, GET /api/environments/{envId}/services/{name}, POST /api/environments/{envId}/services/{name}/test per api-contracts.md §7
- [x] T100 [P] [US6] Implement service API hooks in `src/NatsManager.Frontend/src/features/services/hooks/`: `useServices.ts`, `useService.ts`, `useTestServiceRequest.ts`
- [x] T101 [P] [US6] Implement service components in `src/NatsManager.Frontend/src/features/services/components/`: `ServiceList.tsx` (with availability indicators, "auto-discovered" badges per FR-029), `ServiceDetail.tsx` (endpoints, groups, health, metadata), `ServiceTestDialog.tsx` (side-effect warning, payload input, response display)
- [x] T102 [US6] Create service types in `src/NatsManager.Frontend/src/features/services/types.ts` and add service routes to `src/NatsManager.Frontend/src/App.tsx`

**Checkpoint**: User Story 6 complete — service discovery and testing operational

---

## Phase 9: User Story 7 — Manage Object Store buckets and objects (Priority: P7)

**Goal**: Operators can browse Object Store buckets, inspect objects, and upload/download/delete with size warnings and audit logging.

**Independent Test**: Browse Object Store buckets, inspect object metadata, perform upload/download/delete with confirmation prompts.

### Tests for User Story 7

- [x] T103 [P] [US7] Unit tests for Object Store commands/queries in `tests/NatsManager.Application.Tests/Modules/ObjectStore/`: GetObjectBucketsQueryTests.cs, GetObjectsQueryTests.cs, UploadObjectCommandTests.cs, DeleteObjectCommandTests.cs
- [x] T104 [P] [US7] Integration tests for NATS Object Store adapter in `tests/NatsManager.Infrastructure.Tests/Nats/ObjectStoreAdapterTests.cs`: list buckets, list objects, get/put/delete object
- [x] T105 [P] [US7] Contract tests for Object Store endpoints in `tests/NatsManager.Web.Tests/Endpoints/ObjectStoreEndpointTests.cs`: per api-contracts.md §6
- [x] T106 [P] [US7] Frontend tests in `src/NatsManager.Frontend/src/features/objectstore/`: `ObjectBucketList.test.tsx`, `ObjectList.test.tsx`, `ObjectDetail.test.tsx`

### Implementation for User Story 7

- [x] T107 [P] [US7] Implement Object Store observed DTOs in `src/NatsManager.Application/Modules/ObjectStore/Models/`: `ObjectBucketInfo.cs`, `ObjectInfo.cs` per data-model.md
- [x] T108 [US7] Implement NATS Object Store adapter in `src/NatsManager.Infrastructure/Nats/ObjectStoreAdapter.cs` with `IObjectStoreAdapter` port in `src/NatsManager.Application/Modules/ObjectStore/Ports/IObjectStoreAdapter.cs`: list buckets, list objects, get object metadata, download object, upload object, delete object, create/delete bucket
- [x] T109 [US7] Implement Object Store queries in `src/NatsManager.Application/Modules/ObjectStore/Queries/`: `GetObjectBucketsQuery.cs`, `GetObjectBucketDetailQuery.cs`, `GetObjectsQuery.cs`, `GetObjectDetailQuery.cs`
- [x] T110 [US7] Implement Object Store commands in `src/NatsManager.Application/Modules/ObjectStore/Commands/`: `CreateObjectBucketCommand.cs`, `DeleteObjectBucketCommand.cs`, `UploadObjectCommand.cs`, `ReplaceObjectCommand.cs`, `DeleteObjectCommand.cs` (with size warnings for large objects per FR-025)
- [x] T111 [US7] Implement Object Store API endpoints in `src/NatsManager.Web/Endpoints/ObjectStoreEndpoints.cs`: all endpoints per api-contracts.md §6 including download (application/octet-stream) and upload (multipart/form-data)
- [x] T112 [P] [US7] Implement Object Store API hooks in `src/NatsManager.Frontend/src/features/objectstore/hooks/`: `useObjectBuckets.ts`, `useObjectBucket.ts`, `useObjects.ts`, `useObject.ts`, `useUploadObject.ts`, `useDeleteObject.ts`, `useDownloadObject.ts`
- [x] T113 [P] [US7] Implement Object Store components in `src/NatsManager.Frontend/src/features/objectstore/components/`: `ObjectBucketList.tsx`, `ObjectBucketDetail.tsx`, `ObjectList.tsx` (name, size, metadata), `ObjectDetail.tsx` (metadata, size, digest), `ObjectUploadDialog.tsx`, `ObjectSizeWarning.tsx` (large object download warning)
- [x] T114 [US7] Create Object Store types in `src/NatsManager.Frontend/src/features/objectstore/types.ts` and add routes to `src/NatsManager.Frontend/src/App.tsx`

**Checkpoint**: User Story 7 complete — Object Store fully manageable

---

## Phase 10: User Story 8 — Inspect Core NATS subjects, clients, and message traffic (Priority: P8)

**Goal**: Operators can view connected clients, subject hierarchies, traffic characteristics, and publish/subscribe to subjects.

**Independent Test**: Connect to an environment, browse subjects and clients, publish/subscribe to a test subject.

### Tests for User Story 8

- [x] T115 [P] [US8] Unit tests for Core NATS queries/commands in `tests/NatsManager.Application.Tests/Modules/CoreNats/`: GetCoreStatusQueryTests.cs, GetSubjectsQueryTests.cs, PublishMessageCommandTests.cs, SubscribeCommandTests.cs
- [x] T116 [P] [US8] Integration tests for NATS monitoring adapter in `tests/NatsManager.Infrastructure.Tests/Nats/CoreNatsAdapterTests.cs`: server info, subject listing, publish, subscribe
- [x] T117 [P] [US8] Contract tests for Core NATS endpoints in `tests/NatsManager.Web.Tests/Endpoints/CoreNatsEndpointTests.cs`: per api-contracts.md §2
- [x] T118 [P] [US8] Frontend tests in `src/NatsManager.Frontend/src/features/core-nats/`: `CoreNatsStatus.test.tsx`, `SubjectExplorer.test.tsx`, `PublishDialog.test.tsx`, `SubscriptionViewer.test.tsx`

### Implementation for User Story 8

- [x] T119 [P] [US8] Implement Core NATS observed DTO in `src/NatsManager.Application/Modules/CoreNats/Models/NatsServerInfo.cs` per data-model.md
- [x] T120 [US8] Implement NATS monitoring adapter in `src/NatsManager.Infrastructure/Nats/CoreNatsAdapter.cs` with `ICoreNatsAdapter` port in `src/NatsManager.Application/Modules/CoreNats/Ports/ICoreNatsAdapter.cs`: get server info via `$SYS.REQ.SERVER.PING`, list clients, list subjects, publish message, subscribe to subject, manage inspection subscriptions
- [x] T121 [US8] Implement Core NATS queries in `src/NatsManager.Application/Modules/CoreNats/Queries/`: `GetCoreStatusQuery.cs`, `GetClientsQuery.cs`, `GetSubjectsQuery.cs`, `GetSubscriptionMessagesQuery.cs`
- [x] T122 [US8] Implement Core NATS commands in `src/NatsManager.Application/Modules/CoreNats/Commands/`: `PublishMessageCommand.cs` (with live-traffic warning per FR-011), `CreateInspectionSubscriptionCommand.cs`
- [x] T123 [US8] Implement Core NATS API endpoints in `src/NatsManager.Web/Endpoints/CoreNatsEndpoints.cs`: all endpoints per api-contracts.md §2
- [x] T124 [P] [US8] Implement Core NATS API hooks in `src/NatsManager.Frontend/src/features/core-nats/hooks/`: `useCoreStatus.ts`, `useClients.ts`, `useSubjects.ts`, `usePublishMessage.ts`, `useSubscription.ts`, `useSubscriptionMessages.ts`
- [x] T125 [P] [US8] Implement Core NATS components in `src/NatsManager.Frontend/src/features/core-nats/components/`: `CoreNatsStatus.tsx` (server info, client count, uptime), `SubjectExplorer.tsx` (tree view with subscription counts, traffic indicators), `PublishDialog.tsx` (with traffic impact warning), `SubscriptionViewer.tsx` (live message display with metadata/payload)
- [x] T126 [US8] Create Core NATS types in `src/NatsManager.Frontend/src/features/core-nats/types.ts` and add routes to `src/NatsManager.Frontend/src/App.tsx`

**Checkpoint**: User Story 8 complete — Core NATS inspection and testing operational

---

## Phase 11: User Story 9 — Authenticate, authorize, and audit user actions (Priority: P9)

**Goal**: Admins manage users/roles/permissions; role enforcement across all views; searchable audit log.

**Independent Test**: Create users with different roles, verify permission enforcement, inspect audit log for recorded actions.

### Tests for User Story 9

- [x] T127 [P] [US9] Unit tests for auth commands/queries in `tests/NatsManager.Application.Tests/Modules/Auth/`: LoginCommandTests.cs, CreateUserCommandTests.cs, AssignRoleCommandTests.cs, AuthorizationServiceTests.cs (all 4 roles × production/non-production)
- [x] T128 [P] [US9] Unit tests for audit queries in `tests/NatsManager.Application.Tests/Modules/Audit/`: GetAuditEventsQueryTests.cs (filter by actor, action, resource, date range, source)
- [x] T129 [P] [US9] Contract tests for auth/audit endpoints in `tests/NatsManager.Web.Tests/Endpoints/`: AuthEndpointTests.cs (login/logout/me per api-contracts.md §11), AccessControlEndpointTests.cs (users/roles per §10), AuditEndpointTests.cs (per §9)
- [x] T130 [P] [US9] Frontend tests in `src/NatsManager.Frontend/src/features/auth/`: `UserManagement.test.tsx`, `RoleAssignment.test.tsx`; `src/NatsManager.Frontend/src/features/audit/`: `AuditLog.test.tsx`, `AuditEventDetail.test.tsx`

### Implementation for User Story 9

- [x] T131 [US9] Implement auth commands in `src/NatsManager.Application/Modules/Auth/Commands/`: `LoginCommand.cs`, `LogoutCommand.cs`, `CreateUserCommand.cs`, `UpdateUserCommand.cs`, `DeactivateUserCommand.cs`, `AssignRoleCommand.cs`, `RevokeRoleCommand.cs`
- [x] T132 [US9] Implement auth queries in `src/NatsManager.Application/Modules/Auth/Queries/`: `GetCurrentUserQuery.cs`, `GetUsersQuery.cs`, `GetRolesQuery.cs`
- [x] T133 [US9] Implement user repository in `src/NatsManager.Infrastructure/Persistence/UserRepository.cs` with `IUserRepository` port
- [x] T134 [US9] Implement auth API endpoints in `src/NatsManager.Web/Endpoints/AuthEndpoints.cs`: POST login/logout, GET /api/auth/me per api-contracts.md §11; access control endpoints in `src/NatsManager.Web/Endpoints/AccessControlEndpoints.cs`: users CRUD, role assignments per api-contracts.md §10
- [x] T135 [US9] Implement audit query in `src/NatsManager.Application/Modules/Audit/Queries/GetAuditEventsQuery.cs`: paginated, filterable by actor, action, resource, environment, date range, source (UserInitiated/SystemGenerated per FR-053)
- [x] T136 [US9] Implement audit API endpoint in `src/NatsManager.Web/Endpoints/AuditEndpoints.cs`: GET /api/audit/events per api-contracts.md §9
- [x] T137 [P] [US9] Implement auth frontend hooks in `src/NatsManager.Frontend/src/features/auth/hooks/`: `useUsers.ts`, `useCreateUser.ts`, `useUpdateUser.ts`, `useDeactivateUser.ts`, `useAssignRole.ts`, `useRevokeRole.ts`, `useRoles.ts`
- [x] T138 [P] [US9] Implement user management components in `src/NatsManager.Frontend/src/features/auth/components/`: `UserManagement.tsx` (user list, create/edit modal), `UserDetail.tsx`, `RoleAssignment.tsx` (assign/revoke roles per environment)
- [x] T139 [P] [US9] Implement audit log components in `src/NatsManager.Frontend/src/features/audit/components/`: `AuditLog.tsx` (searchable, filterable by user/action/resource/date/source), `AuditEventDetail.tsx` (full event details with JSON diff for updates)
- [x] T140 [P] [US9] Implement audit API hooks in `src/NatsManager.Frontend/src/features/audit/hooks/`: `useAuditEvents.ts`; create types in `src/NatsManager.Frontend/src/features/audit/types.ts`
- [x] T141 [US9] Add user management and audit routes to `src/NatsManager.Frontend/src/App.tsx` (admin-only routes); integrate role-based action visibility into all existing feature components (StreamActions, ConsumerActions, KvKeyEditor, ObjectUploadDialog, ServiceTestDialog, PublishDialog)

**Checkpoint**: User Story 9 complete — full RBAC and audit trail operational

---

## Phase 12: User Story 10 — Search, filter, and navigate across all resource types (Priority: P10)

**Goal**: Global cross-resource search with type/status filters, bookmarks for quick access, progressive disclosure navigation.

**Independent Test**: Search for known resources across types, verify fast results with correct navigation links.

### Tests for User Story 10

- [x] T142 [P] [US10] Unit tests for search query in `tests/NatsManager.Application.Tests/Modules/Search/GetSearchResultsQueryTests.cs`: cross-type search, filtering by type/status/environment
- [x] T143 [P] [US10] Unit tests for bookmark commands in `tests/NatsManager.Application.Tests/Modules/Shared/`: CreateBookmarkCommandTests.cs, DeleteBookmarkCommandTests.cs, GetBookmarksQueryTests.cs (uniqueness constraint)
- [x] T144 [P] [US10] Contract tests in `tests/NatsManager.Web.Tests/Endpoints/`: SearchEndpointTests.cs (per api-contracts.md §12), BookmarkEndpointTests.cs (per §13), PreferencesEndpointTests.cs (per §15)
- [x] T145 [P] [US10] Frontend tests in `src/NatsManager.Frontend/src/features/search/`: `GlobalSearch.test.tsx`, `SearchResults.test.tsx`, `BookmarkList.test.tsx`

### Implementation for User Story 10

- [x] T146 [US10] Implement search query in `src/NatsManager.Application/Modules/Search/Queries/GetSearchResultsQuery.cs`: query all NATS adapters (streams, consumers, KV buckets, Object Store buckets, services) by name/subject/identifier, aggregate results with type indicators and navigation URLs
- [x] T147 [US10] Implement bookmark commands/queries in `src/NatsManager.Application/Modules/Shared/Commands/`: `CreateBookmarkCommand.cs`, `DeleteBookmarkCommand.cs`; `src/NatsManager.Application/Modules/Shared/Queries/GetBookmarksQuery.cs`; bookmark repository in `src/NatsManager.Infrastructure/Persistence/BookmarkRepository.cs`
- [x] T148 [US10] Implement preferences commands/queries in `src/NatsManager.Application/Modules/Shared/Queries/GetPreferencesQuery.cs`, `src/NatsManager.Application/Modules/Shared/Commands/SetPreferenceCommand.cs`; preferences repository in `src/NatsManager.Infrastructure/Persistence/PreferencesRepository.cs`
- [x] T149 [US10] Implement search, bookmark, and preferences API endpoints in `src/NatsManager.Web/Endpoints/`: `SearchEndpoints.cs` (GET /api/search per §12), `BookmarkEndpoints.cs` (GET/POST/DELETE /api/bookmarks per §13), `PreferencesEndpoints.cs` (GET/PUT /api/preferences per §15)
- [x] T150 [P] [US10] Implement search API hooks in `src/NatsManager.Frontend/src/features/search/hooks/`: `useSearch.ts`, `useBookmarks.ts`, `useCreateBookmark.ts`, `useDeleteBookmark.ts`, `usePreferences.ts`, `useSetPreference.ts`
- [x] T151 [P] [US10] Implement search components in `src/NatsManager.Frontend/src/features/search/components/`: `GlobalSearch.tsx` (command-palette style search bar in AppLayout header), `SearchResults.tsx` (results with type indicators, resource links, environment context), `SearchFilters.tsx` (filter by resource type, status), `BookmarkList.tsx` (sidebar or page with bookmarked resources)
- [x] T152 [US10] Create search types in `src/NatsManager.Frontend/src/features/search/types.ts`; add bookmark button to all resource detail views (StreamDetail, ConsumerDetail, KvBucketDetail, ObjectDetail, ServiceDetail); add global search bar to AppLayout
- [x] T153 [US10] Implement user preferences: default environment, theme, listPageSize — integrate into `src/NatsManager.Frontend/src/features/auth/AuthContext.tsx` and `src/NatsManager.Frontend/src/shared/AppLayout.tsx`

**Checkpoint**: User Story 10 complete — cross-resource search and bookmarks operational

---

## Phase 13: Polish & Cross-Cutting Concerns

**Purpose**: Quality hardening, performance, security, and UX improvements across all stories

- [x] T154 [P] Implement payload inspection shared component in `src/NatsManager.Frontend/src/shared/PayloadViewer.tsx`: raw/structured toggle, truncation indicator, content-type detection, binary content handling, credential masking per FR-034–FR-037; integrate into StreamDetail (message browse), KvKeyDetail, ObjectDetail, SubscriptionViewer
- [x] T155 [P] Implement notification system: `src/NatsManager.Frontend/src/shared/NotificationProvider.tsx` using Mantine Notifications for success/error/warning action outcomes per FR-042, FR-047
- [x] T156 [P] Implement contextual help tooltips in `src/NatsManager.Frontend/src/shared/ContextHelp.tsx` for NATS domain concepts (retention policy, ack policy, delivery policy, etc.) per FR-057
- [x] T157 [P] Implement data source indicator in `src/NatsManager.Frontend/src/shared/DataSourceBadge.tsx`: observed/configured/derived/inferred labels per FR-058
- [x] T158 [P] Add stale data handling: implement `src/NatsManager.Frontend/src/shared/StaleDataBanner.tsx` for degraded/unreachable environments; integrate with EnvironmentContext to show last-known data marked as stale per edge cases
- [x] T159 [P] Implement JetStream-disabled graceful degradation: conditionally hide JetStream, KV, Object Store navigation and dashboard sections when `jetStreamEnabled=false` per edge case; implement in `src/NatsManager.Frontend/src/shared/AppLayout.tsx` conditional nav items
- [x] T160 Implement resource-gone conflict handling: detect 404/410 on destructive actions against already-deleted resources, show informative message and refresh view per edge cases
- [x] T161 [P] Add keyboard accessibility: ensure all interactive elements in shared components are keyboard-navigable with ARIA labels per constitution III
- [x] T162 [P] Verify and enforce route-based code splitting: audit all lazy imports in `src/NatsManager.Frontend/src/App.tsx`, verify initial bundle ≤ 300KB gzipped via `vite build` output
- [x] T163 [P] Add Serilog structured logging configuration in `src/NatsManager.Web/Program.cs`: console + file sinks, correlation IDs for request tracing, audit event logging integration
- [x] T164 Security hardening: enforce HTTPS redirect in production, set secure cookie flags, add CSRF protection, validate `X-Confirm` header on all destructive endpoints, audit credential encryption at rest in `src/NatsManager.Infrastructure/Auth/CredentialEncryptionService.cs`
- [x] T165 Run quickstart.md validation: follow `specs/001-nats-admin-app/quickstart.md` end-to-end (Aspire AppHost start, register environment, browse streams, CRUD operations) and fix any issues

---

## Phase 14: E2E Testing (Playwright + Aspire Testing)

**Purpose**: End-to-end browser tests using Playwright driven by Aspire.Hosting.Testing to spin up the full distributed application stack

- [x] T166 Fix DatabaseInitializer seed: hash default admin password using `PasswordHasher.Hash("admin")` instead of storing raw string (bug: login was impossible with unhashed password)
- [x] T167 Create E2E test project `tests/NatsManager.E2E.Tests/NatsManager.E2E.Tests.csproj` with Aspire.Hosting.Testing, Microsoft.Playwright, xunit.v3.mtp-v2, FluentAssertions; reference AppHost project
- [x] T168 [P] Implement `E2EFixture` in `tests/NatsManager.E2E.Tests/Infrastructure/E2EFixture.cs`: starts Aspire distributed app via `DistributedApplicationTestingBuilder`, waits for backend/frontend healthy, launches Playwright Chromium
- [x] T169 [P] Implement `E2ETestBase` in `tests/NatsManager.E2E.Tests/Infrastructure/E2ETestBase.cs`: creates isolated browser context/page per test, provides `LoginAsAdminAsync()` and `NavigateAsync()` helpers
- [x] T170 [P] Implement `E2ECollection` in `tests/NatsManager.E2E.Tests/Infrastructure/E2ECollection.cs`: xUnit collection definition sharing E2EFixture across all test classes
- [x] T171 [P] Implement login E2E tests in `tests/NatsManager.E2E.Tests/Tests/LoginTests.cs`: login form render, valid credentials redirect, invalid credentials error, unauthenticated redirect
- [x] T172 [P] Implement environment management E2E tests in `tests/NatsManager.E2E.Tests/Tests/EnvironmentTests.cs`: environment list page, register new environment, connection status display
- [x] T173 [P] Implement dashboard navigation E2E tests in `tests/NatsManager.E2E.Tests/Tests/DashboardNavigationTests.cs`: dashboard loads, sidebar items visible, navigate to JetStream/KV/Audit, header branding and user display

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phases 3–12)**: All depend on Foundational phase completion
  - User stories can proceed in priority order (P1 → P2 → ... → P10)
  - Or in parallel if staffed (after Foundational)
  - US3 depends on US2 (adds write operations to JetStream read views)
  - US5 depends on US2 + US4 (dashboard aggregates data from both)
  - US9 audit integration touches all prior feature components
  - US10 search aggregates data from US2, US4, US6, US7, US8
  - All other stories are independent after Foundational
- **Polish (Phase 13)**: Depends on all user stories being complete
- **E2E Testing (Phase 14)**: Depends on Polish phase — requires full app functional

### User Story Dependencies

- **US1 (P1)**: Foundational only — standalone MVP
- **US2 (P2)**: Foundational only — independent read views
- **US3 (P3)**: Depends on US2 (adds CRUD to existing stream/consumer views)
- **US4 (P4)**: Foundational only — independent KV views
- **US5 (P5)**: Depends on US2 + US4 (aggregates data for dashboard; Object Store + Services data can be placeholder until US6/US7)
- **US6 (P6)**: Foundational only — independent service discovery
- **US7 (P7)**: Foundational only — independent Object Store views
- **US8 (P8)**: Foundational only — independent Core NATS views
- **US9 (P9)**: Foundational only for core auth; integration step (T141) touches all prior components
- **US10 (P10)**: Depends on US2, US4, US6, US7, US8 (search aggregates all resources)

### Within Each User Story

- Tests written first (fail before implementation)
- DTOs/models before adapters
- Adapters before queries/commands
- Queries/commands before API endpoints
- API hooks before UI components
- Integration/routing last

### Parallel Opportunities

**Phase 1 (Setup)**: T003, T004, T005, T006, T007, T008, T009, T010, T011, T012 all parallelizable after T001–T002

**Phase 2 (Foundational)**: T015–T018 parallel (domain entities); T021–T028 parallel (infrastructure); T031–T034 parallel (frontend shell)

**Within User Stories**: Backend tests + frontend tests parallel; API hooks + UI components parallel; independent stories parallel after Foundational

---

## Parallel Example: User Story 2

```
# Worker A: Backend
T050: Unit tests for JetStream queries (FAIL first)
T054: JetStream DTOs
T055: JetStream adapter
T056: JetStream queries
T057: JetStream endpoints
T051: Integration tests (verify)
T052: Contract tests (verify)

# Worker B: Frontend (can start once T057 is ready or use MSW mocks)
T053: Frontend component tests (FAIL first)
T060: JetStream types
T058: JetStream API hooks
T059: JetStream components
T061: Routes integration
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test US1 independently — register environment, see status
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Environment management (MVP!)
3. US2 → JetStream browsing → Deploy/Demo
4. US3 → JetStream CRUD with safeguards
5. US4 → KV Store management
6. US5 → Dashboard (single pane of glass)
7. US6–US8 → Services, Object Store, Core NATS
8. US9 → Auth/audit hardening
9. US10 → Search and bookmarks
10. Polish → Production-ready

### Parallel Team Strategy

With multiple developers after Foundational completion:
- Developer A: US1 (environments) → US5 (dashboard)
- Developer B: US2 (JetStream read) → US3 (JetStream CRUD)
- Developer C: US4 (KV Store) → US6 (Services)
- Developer D: US7 (Object Store) → US8 (Core NATS)
- Then converge: US9 (auth) → US10 (search) → Polish

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Tests fail before implementing
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Frontend tests are colocated: `*.test.tsx` next to source in `src/NatsManager.Frontend/src/features/`
- Backend tests are in `tests/` directory (xUnit projects)
- All destructive endpoints require `X-Confirm: true` header
- All NATS data includes freshness headers (`X-Data-Freshness`, `X-Data-Timestamp`)

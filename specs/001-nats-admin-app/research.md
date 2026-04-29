# Research: NATS Admin Application

**Branch**: `001-nats-admin-app`
**Date**: 2026-04-06
**Purpose**: Consolidate technology decisions, best practices, and rationale for the implementation plan.

---

## 1. Architecture Pattern: Modular Monolith

**Decision**: Single-container modular monolith with 9 bounded context modules.

**Rationale**: The product must run as one lightweight container suitable for Docker, Podman, and Kubernetes. A modular monolith provides clear internal boundaries (DDD bounded contexts) while avoiding the operational overhead of microservices. The architecture supports future decomposition if scale demands it.

**Alternatives considered**:
- **Microservices**: Rejected — operational complexity disproportionate for a management tool. The product is an administration UI, not a high-throughput data pipeline.
- **Simple monolith without module boundaries**: Rejected — NATS has 5+ distinct capability areas that benefit from explicit domain separation to avoid coupling.

---

## 2. Backend Stack: .NET 10 / ASP.NET Core 10

**Decision**: C# with .NET 10, ASP.NET Core Minimal APIs, EF Core 10, SQLite.

**Rationale**: .NET 10 provides native AOT compatibility for smaller container images, excellent performance for API workloads, and strong typing. Minimal APIs reduce boilerplate compared to MVC controllers while maintaining full ASP.NET Core middleware stack. EF Core provides migration management and LINQ-based queries for SQLite.

**Alternatives considered**:
- **Go + HTMX**: Rejected — while lightweight, the functional spec demands rich interactive UIs (confirmation flows, real-time updates, progressive disclosure) better served by a React SPA.
- **Node.js + Express**: Rejected — .NET provides stronger typing at the domain layer, which aligns with the Code Quality constitution principle (explicit type definitions).

**Best practices for .NET 10 in this domain**:
- Use `IOptions<T>` pattern for per-environment NATS connection configuration
- Use `IHostedService` / `BackgroundService` for periodic health polling and metadata refresh
- Use `CancellationToken` propagation on all NATS operations to enforce bounded timeouts (max 10s per constitution)
- Use the repository's lightweight in-process `IUseCase<TRequest, TResult>` / `IOutputPort<T>` pattern for CQRS command/query dispatch
- Use `FluentValidation` for command input validation at the application boundary
- Use `Serilog` with structured logging for correlation of actions and audit events

---

## 3. NATS .NET Client Integration

**Decision**: Use `NATS.Net` (official NATS .NET client v2) for all NATS interactions.

**Rationale**: The official client provides full support for Core NATS, JetStream, KV Store, Object Store, and service micro APIs. It supports async/await natively and connects behind application ports as required by hexagonal architecture.

**Key integration patterns**:
- **Connection per environment**: Each registered environment gets its own `NatsConnection` instance managed through a factory (`INatsConnectionFactory`). Connections are lazily initialized and health-checked periodically.
- **JetStream management**: Use `INatsJSContext` for stream/consumer CRUD; `INatsJSStream` and `INatsJSConsumer` for state inspection.
- **KV Store**: Use `INatsKVContext` to obtain `INatsKVStore` per bucket. Keys support `Get`, `Put`, `Delete`, `History`, and `Watch`.
- **Object Store**: Use `INatsObjContext` to obtain `INatsObjStore` per bucket. Objects support `Get`, `Put`, `Delete`, and metadata listing.
- **Service discovery**: Use the NATS service micro API (`$SRV.INFO`, `$SRV.PING`, `$SRV.STATS`) via request-reply to discover services.
- **Monitoring**: Use NATS system subjects (`$SYS.REQ.SERVER.PING`, `$SYS.REQ.ACCOUNT.*`) for server/account monitoring when available.

**Adapter boundary**: All NATS client usage is encapsulated in `NatsManager.Infrastructure` behind port interfaces defined in `NatsManager.Application`. The domain layer never references NATS client types.

**Alternatives considered**:
- **Direct HTTP to NATS monitoring endpoint**: Rejected — NATS system subjects via the client provide richer operational data and work across all deployment models.
- **Custom protocol adapter**: Rejected — unnecessary when the official client covers all required capabilities.

---

## 4. Frontend Stack: React + TypeScript + Mantine + Vite + Vitest

**Decision**: React 19 SPA with TypeScript strict mode, Mantine 9 component library, Recharts for charts. **Vite** as the build tool and dev server. **Vitest** as the frontend test runner with React Testing Library.

**Rationale**: React is the industry standard for complex interactive applications. Mantine provides a comprehensive component set (tables, modals, navigation, forms) that enforces visual consistency — directly supporting the UX Consistency constitution principle. TypeScript strict mode enforces the Code Quality principle (no `any` types). Recharts integrates with Mantine's design tokens for dashboard charts. Vite provides fast HMR during development and optimized production builds. Vitest shares Vite's configuration and transform pipeline, enabling seamless test execution with the same module resolution.

**Key frontend patterns**:
- **Feature-based module structure**: Each NATS capability area is a folder under `src/features/` with its own components, hooks, and types. This mirrors the backend bounded contexts.
- **Shared components**: `ResourceListView`, `ResourceDetailView`, `ConfirmActionDialog`, `DataFreshnessIndicator`, `EnvironmentBadge` — reused across all capability areas to enforce consistency.
- **API client**: Use TanStack Query (React Query) for server state management with automatic cache invalidation on mutations.
- **State management**: TanStack Query for server state; React Context for local UI state (selected environment, user session). No Redux.
- **Virtualization**: Use `@tanstack/react-virtual` for lists exceeding 100 items to meet the 1,000-item performance requirement.
- **Code splitting**: Route-based lazy loading per feature module to meet the ≤300KB initial bundle target.
- **Build tool**: Vite with `vite.config.ts` for development server (port 5173 with API proxy) and optimized production builds.
- **Testing**: Vitest + React Testing Library. Tests colocated with source as `*.test.ts(x)`. Configuration in `vitest.config.ts` sharing Vite's resolve/alias configuration.

**Alternatives considered**:
- **Angular**: Rejected — heavier framework with steeper learning curve; React + Mantine provides equivalent capability with a smaller bundle.
- **Vue 3**: Rejected — smaller ecosystem for enterprise admin UIs; Mantine is React-native.
- **Ant Design**: Rejected — heavier CSS bundle; Mantine is lighter and provides better TypeScript integration.
- **webpack/CRA**: Rejected — Vite provides faster HMR and build times with simpler configuration.
- **Jest**: Rejected — Vitest shares Vite's transform pipeline, avoiding duplicate configuration. Jest requires separate Babel/TS setup.

---

## 5. Persistence: SQLite with EF Core

**Decision**: SQLite for all application-owned data; NATS is the sole source of truth for runtime resources.

**Rationale**: SQLite provides zero-configuration embeddable persistence suitable for a single-container deployment. It handles the write patterns of a management tool (environment CRUD, audit logging, user preferences) with ease. NATS runtime state (streams, consumers, KV, objects, services) is always read live from NATS to avoid stale cache issues.

**Key implementation patterns**:
- **WAL mode**: Enable WAL (Write-Ahead Logging) for better concurrent read performance during dashboard queries.
- **Migration management**: EF Core migrations for schema evolution.
- **Volume mount**: SQLite database file stored on a persistent volume or bind mount (not in the container writable layer).
- **Audit table partitioning**: Consider date-based partitioning strategy for audit events if retention grows large.

**What is persisted in SQLite**:
- Environment registrations (name, URL, credentials reference, enabled/disabled)
- User accounts and role assignments
- Audit events (who, what, when, which resource, outcome)
- Saved bookmarks and user preferences
- Application configuration

**What is NOT persisted in SQLite**:
- Stream/consumer state (read from NATS JetStream)
- KV keys/values (read from NATS KV)
- Objects (read from NATS Object Store)
- Service metadata (discovered from NATS services)
- Connection status (observed live)

**Alternatives considered**:
- **PostgreSQL**: Rejected — adds deployment complexity (separate database service) contrary to single-container goal.
- **LiteDB**: Rejected — weaker migration story and less community support than EF Core + SQLite.

---

## 6. CQRS Implementation

**Decision**: CQRS at the application boundary with separate command and query handlers.

**Rationale**: Read and write use cases differ significantly in shape and risk. Queries return rich read models for UI consumption; commands express user intent and enforce authorization, validation, and audit logging. This separation makes destructive operations explicit and testable.

**Implementation approach**:
- Use the repository's lightweight `IUseCase<TRequest, TResult>` / `IOutputPort<T>` pattern for dispatching commands and queries.
- Commands return result types (success/failure/warnings) — never rich query graphs.
- Queries are read-only and never modify state.
- Command handlers enforce: input validation → authorization → business rules → NATS action → audit event → result.
- Query handlers may read from SQLite (bookmarks, audit) and/or from NATS (live resource state).

**Alternatives considered**:
- **No CQRS (unified service layer)**: Rejected — conflates inspection with administration, making it harder to enforce the safety requirements.
- **Event sourcing**: Rejected — over-engineering for a management tool. The application is not the source of truth for NATS state.

---

## 7. Authentication & Authorization

**Decision**: Cookie-based session authentication with role-based authorization (RBAC). Four roles: ReadOnly, Operator, Administrator, Auditor.

**Rationale**: The functional spec requires authentication (FR-048), role-based access (FR-049), and environment-scoped restrictions (FR-050). Cookie-based sessions are simple for a browser SPA deployed on an internal network. RBAC provides clear permission boundaries.

**Role matrix**:

| Role | View resources | Modify resources | Destructive actions | Manage users | View audit |
|------|---------------|-----------------|--------------------| -------------|-----------|
| ReadOnly | ✅ | ❌ | ❌ | ❌ | ❌ |
| Operator | ✅ | ✅ | ❌ in production | ❌ | ❌ |
| Administrator | ✅ | ✅ | ✅ (with confirmation) | ✅ | ✅ |
| Auditor | ✅ | ❌ | ❌ | ❌ | ✅ |

**Environment-level restrictions**: Administrators can mark environments as "production" to enforce additional confirmation and restrict Operator destructive actions.

**Alternatives considered**:
- **OAuth2/OIDC**: Viable for future integration but adds complexity for v1 internal deployment. Can be added as an alternative auth adapter behind the same application port.
- **JWT tokens**: Rejected for primary auth — cookies are simpler for SPA same-origin deployment and avoid token refresh complexity.

---

## 8. Audit Logging

**Decision**: All state-changing actions produce an audit event persisted in SQLite with structured fields.

**Rationale**: FR-051 requires recording who, what, when, which resource, and outcome for all state-changing operations. SQLite provides queryable storage with search/filter support (FR-052).

**Audit event structure**:
- `Id` (GUID)
- `Timestamp` (UTC)
- `ActorId` (user reference)
- `ActorName` (denormalized for readability)
- `ActionType` (enum: Create, Update, Delete, TestInvoke, Publish, Subscribe, Login, Logout, PermissionChange)
- `ResourceType` (enum: Environment, Stream, Consumer, KvBucket, KvKey, ObjectBucket, Object, Service, User, Role)
- `ResourceId` (string identifier)
- `ResourceName` (denormalized)
- `EnvironmentId` (which NATS environment)
- `Outcome` (Success, Failure, Warning)
- `Details` (JSON blob for action-specific context)
- `Source` (UserInitiated, SystemGenerated)

---

## 9. Real-Time Updates

**Decision**: Polling-based refresh with configurable intervals; WebSocket/SSE for future enhancement.

**Rationale**: NATS system subjects provide point-in-time snapshots, not continuous streams of state changes. Polling at 10–30 second intervals for dashboards and on-demand refresh for detail views provides a practical balance. The UI uses TanStack Query's `refetchInterval` for automatic staleness detection.

**Data freshness indicators**: Every data fetch records its timestamp. The UI displays "Live", "Updated Xs ago", or "Stale" based on age thresholds (live < 5s, recent < 30s, stale > 30s).

**Alternatives considered**:
- **WebSocket push from backend**: Can be added later for high-frequency monitoring but adds complexity for v1.
- **NATS JetStream watch**: Useful for KV key changes but doesn't cover all resource types.

---

## 10. Containerization & Development Orchestration

**Decision**: Multi-stage Dockerfile for production; .NET Aspire AppHost for development orchestration (replaces docker-compose).

**Rationale**: Single-container deployment is a core architectural requirement. Multi-stage build compiles both .NET backend and React frontend (via Vite), then copies published artifacts to a runtime-only base image. For development, .NET Aspire provides integrated orchestration of NATS, backend, and frontend with service discovery, health monitoring, and the Aspire Dashboard for telemetry.

**Production build stages** (Dockerfile in `src/NatsManager.Web/`):
1. `node:lts-alpine` — `npm ci && npm run build` (Vite production build), emit static assets to `dist/`
2. `mcr.microsoft.com/dotnet/sdk:10.0` — restore, build, publish ASP.NET Core app
3. `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` — runtime image with published app + static assets

**Runtime configuration**:
- Environment variables for application settings
- Mounted volume at `/data` for SQLite database
- Health probe at `/health`
- Port 8080 (HTTP)

**Development orchestration** (Aspire AppHost):
- `NatsManager.AppHost` project orchestrates all development components
- NATS container with JetStream: `builder.AddNats("nats").WithArgs("-js")` (via `Aspire.Hosting.Nats`)
- Backend: `builder.AddProject<Projects.NatsManager_Web>("backend").WithReference(nats)`
- Frontend: `builder.AddViteApp("frontend", "../NatsManager.Frontend").WithReference(backend)` (via `Aspire.Hosting.JavaScript`)
- Aspire Dashboard provides real-time resource monitoring, structured logs, traces, and metrics
- No docker-compose.yml — Aspire AppHost is the single entry point for development

**Alternatives considered**:
- **docker-compose**: Rejected — Aspire provides richer development experience with integrated dashboard, health checks, automatic service discovery, and OpenTelemetry. docker-compose requires manual port mapping and has no telemetry integration.
- **Tye**: Rejected — deprecated in favor of Aspire.

---

## 11. Error Handling & Resilience

**Decision**: Structured error responses with domain-specific error types; circuit-breaker pattern for NATS connections.

**Rationale**: The constitution requires actionable error feedback (FR-056) and graceful degradation when NATS is unreachable. The UI must never show blank screens or silent failures.

**Patterns**:
- **API errors**: Return structured `ProblemDetails` (RFC 9457) with `type`, `title`, `detail`, and `instance`.
- **NATS connectivity**: Connection factory implements health-check with timeout. Failed connections report "unavailable" without blocking other environments.
- **Optimistic concurrency**: For KV key updates, use NATS revision numbers. Return conflict error if the key was modified between read and write.
- **UI error boundaries**: React Error Boundaries per feature module prevent one module's failure from crashing the entire application.

---

## 12. .NET Aspire Integration

**Decision**: Use .NET Aspire for development orchestration with two dedicated projects: `NatsManager.AppHost` and `NatsManager.ServiceDefaults`.

**Rationale**: Aspire provides a first-class development experience for multi-component applications with integrated service discovery, health monitoring, and OpenTelemetry telemetry. It replaces docker-compose with a programmatic, type-safe orchestration model. The Aspire Dashboard gives developers visibility into logs, traces, and metrics across all components during development.

**AppHost project** (`NatsManager.AppHost`):
- References `Aspire.Hosting.Nats` for NATS container orchestration
- References `Aspire.Hosting.JavaScript` for Vite frontend orchestration
- Registers NATS container: `builder.AddNats("nats").WithArgs("-js")` (enables JetStream via container args since no `WithJetStream()` extension exists)
- Registers backend: `builder.AddProject<Projects.NatsManager_Web>("backend").WithReference(nats)`
- Registers frontend: `builder.AddViteApp("frontend", "../NatsManager.Frontend").WithReference(backend)`
- `WithReference()` injects connection strings and service endpoints automatically via environment variables
- `WaitFor()` ensures startup ordering (backend waits for NATS, frontend waits for backend)

**ServiceDefaults project** (`NatsManager.ServiceDefaults`):
- Shared project referenced by `NatsManager.Web` (and any future service projects)
- Provides `AddServiceDefaults()` extension method called in `Program.cs`
- Configures OpenTelemetry (tracing, metrics, structured logging) with OTLP exporter for Aspire Dashboard
- Registers standard health check endpoints (`/health`, `/alive`)
- Configures HTTP client resilience (retry, circuit-breaker) via `Microsoft.Extensions.Http.Resilience`
- Enables service discovery for inter-service communication

**NATS client integration** (in `NatsManager.Web`):
- Package: `Aspire.NATS.Net` provides `builder.AddNatsClient("nats")` which registers `INatsConnection` in DI
- Health checks are automatically configured by the Aspire NATS component
- Connection string is injected from the AppHost via `WithReference(nats)`

**Alternatives considered**:
- **docker-compose only**: Rejected — no integrated dashboard, manual port configuration, no health check orchestration, no OpenTelemetry integration.
- **TypeScript AppHost**: Rejected — C# AppHost is the standard and well-supported pattern; TypeScript AppHost is newer and less documented. The backend is already .NET, so C# AppHost is natural.

---

## 13. Vite Build Tool

**Decision**: Use Vite as the frontend build tool and development server.

**Rationale**: Vite provides near-instant HMR (Hot Module Replacement) during development via native ES modules, and uses Rollup for optimized production builds. It has first-class TypeScript and React support via `@vitejs/plugin-react`. The Aspire AppHost integrates with Vite via `AddViteApp()`.

**Configuration** (`vite.config.ts`):
- `@vitejs/plugin-react` for React Fast Refresh
- Proxy `/api` requests to backend during standalone development (without Aspire)
- Output to `dist/` for production builds
- Define `build.outDir` for Dockerfile COPY stage

**Alternatives considered**:
- **webpack / Create React App**: Rejected — slower HMR, more complex configuration, CRA is deprecated.
- **Turbopack**: Rejected — still experimental, less ecosystem integration.

---

## 14. Vitest Test Runner

**Decision**: Use Vitest as the frontend test runner with React Testing Library.

**Rationale**: Vitest shares Vite's configuration, module resolution, and transform pipeline. This eliminates the dual-config problem (separate Babel/TS config for tests) that Jest requires when paired with Vite. Vitest supports the same `describe/it/expect` API as Jest, making it familiar. React Testing Library provides component testing with DOM assertions.

**Configuration** (`vitest.config.ts`):
- Extends or shares Vite config for consistent module resolution
- `environment: 'jsdom'` for DOM simulation
- `setupFiles` for React Testing Library's `jest-dom` matchers
- `include: ['src/**/*.test.{ts,tsx}']` for colocated test discovery
- Coverage via `@vitest/coverage-v8`

**Test conventions**:
- Tests colocated with source: `ComponentName.tsx` → `ComponentName.test.tsx`
- Test names describe scenario and expected outcome (per constitution II)
- Mocks only at system boundaries (API calls via MSW or TanStack Query mocking)
- 80% coverage target for new frontend code

**Alternatives considered**:
- **Jest**: Rejected — requires separate Babel/TS configuration when using Vite. Vitest provides identical API with native Vite integration.
- **Playwright Component Testing**: Rejected — heavier setup; Vitest + RTL covers component testing needs. Playwright reserved for potential E2E tests.

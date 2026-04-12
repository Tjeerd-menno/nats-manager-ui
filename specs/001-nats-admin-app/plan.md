# Implementation Plan: NATS Admin Application

**Branch**: `001-nats-admin-app` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-nats-admin-app/spec.md`

## Summary

Build a full-stack NATS administration web application providing a unified management UI for Core NATS, JetStream (streams/consumers), Key-Value Store, Object Store, and NATS Services. The backend uses .NET 10 with ASP.NET Core Minimal APIs and a modular monolith architecture (9 bounded contexts). The frontend uses React 19 with Mantine 7, built with Vite. Local development is orchestrated via .NET Aspire AppHost. Production deploys as a single OCI container.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core 10 (Minimal APIs), EF Core 10, NATS.Net v2, MediatR, FluentValidation, Serilog, React 19, Mantine 7, Recharts, TanStack Query, @tanstack/react-virtual, Vite, Vitest  
**Dev Orchestration**: .NET Aspire (AppHost + ServiceDefaults) — development only; replaces docker-compose  
**Storage**: SQLite (application data: environments, users, audit, bookmarks, preferences); NATS (live resource state)  
**Testing**: xUnit + FluentAssertions (backend), Vitest + React Testing Library (frontend, colocated `*.test.ts(x)`), contract tests at API boundary  
**Target Platform**: Desktop browsers (internal network / VPN); Linux OCI container for production  
**Project Type**: Web application (SPA + API)  
**Performance Goals**: FCP ≤ 1.5s, TTI ≤ 2.0s, API p95 ≤ 1s, 1k-item list render ≤ 200ms, JS bundle ≤ 300KB gzipped  
**Constraints**: View navigation ≤ 500ms, NATS timeouts bounded at 10s, memory growth ≤ 5MB/hour, single-container production deployment  
**Scale/Scope**: Up to 10,000 resources per environment, 10 user stories, 58 functional requirements, 5 NATS capability areas

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality (NON-NEGOTIABLE) — ✅ PASS

- Single responsibility: 9 bounded context modules each with clear domain scope
- Linting/formatting: ESLint + Prettier (frontend), `dotnet format` + analyzers (backend)
- Function size: ≤ 40 lines enforced by code review
- NATS domain terminology: UI labels match NATS docs exactly (streams, consumers, buckets, subjects)
- No `any` types: TypeScript strict mode enforced
- Dependencies version-pinned, justified in project files

### II. Testing Standards (NON-NEGOTIABLE) — ✅ PASS

- Unit tests: xUnit (backend), Vitest (frontend), 80% coverage target
- Integration tests: containerized NATS via Aspire for backend; React Testing Library for frontend
- Contract tests: API boundary validation
- Deterministic tests: no flaky tests in main branch
- Destructive operation safeguards have dedicated test coverage
- Test naming: scenario + expected outcome convention
- Mocks only at system boundaries

### III. User Experience Consistency — ✅ PASS

- Consistent navigation across all 5 NATS capability areas
- Shared components: `ResourceListView`, `ResourceDetailView`, `ConfirmActionDialog`, `DataFreshnessIndicator`
- Detail views follow identity → status → configuration → relationships → actions
- Destructive ops require confirmation with resource name + environment context
- All states handled: loading, empty, error, stale

### IV. Performance Requirements — ✅ PASS

- Performance targets defined and measurable (constitution Performance Standards table)
- Virtualization for 1k+ item lists via @tanstack/react-virtual
- Route-based code splitting for ≤ 300KB initial bundle
- Async operations with loading indicators, bounded 10s NATS timeouts
- Polling-based refresh with configurable intervals and freshness indicators

## Project Structure

### Documentation (this feature)

```text
specs/001-nats-admin-app/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api-contracts.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── NatsManager.AppHost/              # .NET Aspire orchestrator (dev only)
│   ├── Program.cs                    # Registers NATS container, backend, frontend
│   └── NatsManager.AppHost.csproj
│
├── NatsManager.ServiceDefaults/      # Shared Aspire defaults (OpenTelemetry, health, resilience)
│   ├── Extensions.cs
│   └── NatsManager.ServiceDefaults.csproj
│
├── NatsManager.Domain/               # Domain entities, value objects, enums
│   └── Modules/
│       ├── Environments/
│       ├── JetStream/
│       ├── KeyValue/
│       ├── ObjectStore/
│       ├── Services/
│       ├── CoreNats/
│       ├── Auth/
│       ├── Audit/
│       └── Shared/
│
├── NatsManager.Application/          # CQRS commands, queries, ports, validators
│   └── Modules/
│       └── [same 9 modules]
│
├── NatsManager.Infrastructure/       # EF Core, NATS adapters, auth implementation
│   ├── Persistence/
│   ├── Nats/
│   └── Auth/
│
├── NatsManager.Web/                  # ASP.NET Core Minimal API host
│   ├── Endpoints/
│   ├── Middleware/
│   ├── Program.cs
│   └── Dockerfile                    # Production container build
│
└── NatsManager.Frontend/             # React 19 SPA (Vite + Vitest)
    ├── src/
    │   ├── features/                 # Feature modules (one per NATS capability)
    │   │   ├── environments/
    │   │   │   ├── components/
    │   │   │   ├── hooks/
    │   │   │   ├── types.ts
    │   │   │   └── *.test.tsx        # Colocated Vitest tests
    │   │   ├── jetstream/
    │   │   ├── kv/
    │   │   ├── objectstore/
    │   │   ├── services/
    │   │   ├── core-nats/
    │   │   ├── dashboard/
    │   │   ├── auth/
    │   │   ├── audit/
    │   │   └── search/
    │   ├── shared/                   # Shared components, hooks, types
    │   ├── api/                      # TanStack Query API client
    │   ├── App.tsx
    │   └── main.tsx
    ├── vite.config.ts
    ├── vitest.config.ts
    ├── tsconfig.json
    └── package.json

tests/
├── NatsManager.Domain.Tests/
├── NatsManager.Application.Tests/
├── NatsManager.Infrastructure.Tests/
└── NatsManager.Web.Tests/            # API integration + contract tests
```

**Structure Decision**: Web application with Aspire orchestration. Backend follows Clean Architecture with 4 .NET projects (Domain, Application, Infrastructure, Web) plus 2 Aspire projects (AppHost, ServiceDefaults). Frontend is a Vite-based React SPA with colocated Vitest tests. Backend tests are in a separate `tests/` directory. No docker-compose.yml — Aspire AppHost manages NATS container, backend, and Vite frontend for development. Dockerfile in `NatsManager.Web` handles production builds.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 6 .NET projects (vs 3) | Clean Architecture requires Domain/Application/Infrastructure/Web separation; Aspire requires AppHost + ServiceDefaults | A single project would mix domain logic with infrastructure concerns, violating single responsibility (Constitution I) |
| MediatR for CQRS | Explicit separation of read-only queries from state-changing commands with audit logging | Direct service calls would conflate inspection with administration, making destructive operations harder to safeguard |

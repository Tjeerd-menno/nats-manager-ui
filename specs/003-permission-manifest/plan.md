# Implementation Plan: Application Permission Exposure

**Branch**: `003-permission-manifest` | **Date**: 2026-05-23 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-permission-manifest/spec.md`

## Summary

Add a backend-only permission manifest module that publishes the application's active permissions at `/.well-known/permissions`. The manifest is code-owned by the application, validated before publication, cached as the last valid manifest, and returned through an ASP.NET Core Minimal API endpoint. Endpoint exposure is explicitly controlled by deployment configuration: public mode allows unauthenticated discovery, and restricted mode requires a configured service access key.

## Technical Context

**Language/Version**: C# / .NET 10 backend; TypeScript frontend unchanged
**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, `System.Text.Json`, options validation, Serilog, xUnit v3 + Shouldly + NSubstitute, `Microsoft.AspNetCore.Mvc.Testing`
**Storage**: Code-owned static manifest source plus in-memory last-valid publication cache; no database schema change and no NATS calls
**Testing**: xUnit v3 + MTP v2 for application and web tests; `WebApplicationFactory<Program>` for endpoint contract tests
**Target Platform**: Existing Linux OCI-hosted NATS Manager web application; desktop/browser UI unchanged
**Project Type**: Backend feature addition to existing SPA + ASP.NET Core Minimal API web application
**Performance Goals**: Manifest retrieval completes from memory within 100ms p95; startup validation completes within 1s for expected permission sets; no added frontend bundle cost
**Constraints**: Endpoint path is root-level `/.well-known/permissions`, not under `/api`; each deployment must explicitly choose public or restricted exposure; restricted exposure requires service-key validation; only active permissions are published; application version is the only version field; no IAM import, approval, assignment, or policy-enforcement workflow
**Scale/Scope**: One well-known endpoint, one application module, one web endpoint file, one options class, application/web tests; designed for tens to hundreds of permissions with a hard validation guard against duplicate names

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Code Quality (NON-NEGOTIABLE) - PASS

- Single responsibility: manifest models, validation, publication cache, access policy, and endpoint mapping are separate concerns.
- Existing project structure is preserved; no new project or cross-module ownership change.
- All public manifest DTOs use explicit records and nullable annotations; no `any` or untyped payloads.
- Permission terminology is specific to application authorization and does not reuse NATS resource terms incorrectly.
- No new external dependencies are required; existing framework, options, logging, and test packages are sufficient.

### II. Testing Standards (NON-NEGOTIABLE) - PASS

- Unit tests will cover manifest validation, duplicate permission rejection, active-only permission filtering, last-valid fallback, and access-policy decisions.
- Web tests will cover `/.well-known/permissions` status codes, JSON shape, restricted access, invalid current manifest fallback, and no-prior-valid failure.
- Contract tests will validate the endpoint response shape and headers without relying on implementation details.
- Mocks remain at system boundaries through existing `WebApplicationFactory` and service substitutions.

### III. User Experience Consistency - PASS

- No UI changes are required.
- Error responses use existing problem-result conventions where the endpoint cannot return a valid manifest.
- Operational failures are logged with structured fields so operators can diagnose manifest validation and retrieval issues.

### IV. Performance Requirements - PASS

- Retrieval is served from an in-memory validated snapshot, avoiding database, NATS, and filesystem work on the request path.
- Startup validation and manifest publication are bounded by permission list size.
- The feature adds no frontend code and therefore no bundle, rendering, or navigation regression.

## Project Structure

### Documentation (this feature)

```text
specs/003-permission-manifest/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── spec.md
├── checklists/
│   └── requirements.md
└── contracts/
    └── api-contracts.md
```

### Source Code (repository root)

```text
src/
├── NatsManager.Application/
│   └── Modules/
│       └── Permissions/
│           ├── Models/
│           │   └── PermissionManifestModels.cs
│           ├── Queries/
│           │   └── GetPermissionManifestQuery.cs
│           └── Services/
│               ├── PermissionManifestRegistry.cs
│               ├── PermissionManifestValidator.cs
│               └── PermissionManifestPublisher.cs
│
└── NatsManager.Web/
    ├── Configuration/
    │   └── PermissionManifestOptions.cs
    ├── Endpoints/
    │   └── PermissionManifestEndpoints.cs
    └── Program.cs

tests/
├── NatsManager.Application.Tests/
│   └── Modules/
│       └── Permissions/
│           ├── PermissionManifestValidatorTests.cs
│           └── PermissionManifestPublisherTests.cs
└── NatsManager.Web.Tests/
    └── Endpoints/
        └── PermissionManifestEndpointTests.cs
```

**Structure Decision**: Use the existing backend Clean Architecture layout. The application layer owns the manifest data, validation, and last-valid publication behavior. The web layer owns deployment options, access policy enforcement, endpoint mapping, and HTTP response details. No frontend files and no persistence files are planned.

## Phase 0 - Research

Completed in [research.md](research.md). All planning unknowns are resolved with decisions for endpoint shape, manifest ownership, validation/fallback behavior, exposure policy, version semantics, and test strategy.

## Phase 1 - Design & Contracts

Completed artifacts:

- [data-model.md](data-model.md) defines manifest entities, validation rules, and publication state transitions.
- [contracts/api-contracts.md](contracts/api-contracts.md) defines the `GET /.well-known/permissions` contract.
- [quickstart.md](quickstart.md) documents developer verification and manual endpoint checks.

## Post-Design Constitution Check

### I. Code Quality (NON-NEGOTIABLE) - PASS

Design keeps manifest logic in one `Permissions` application module and endpoint concerns in one web endpoint file. No duplicate permission-shape definitions are introduced outside generated contract documentation.

### II. Testing Standards (NON-NEGOTIABLE) - PASS

Design includes application unit tests and web contract tests for all functional and edge-case requirements. No untestable behavior remains.

### III. User Experience Consistency - PASS

No UI changes are introduced. Failure states use existing API problem conventions and structured logs for operators.

### IV. Performance Requirements - PASS

Design keeps the request path memory-only and adds no frontend or NATS dependency. The planned verification covers retrieval latency and failure behavior without requiring new performance infrastructure.

## Complexity Tracking

No constitution violations. The feature is additive within existing backend modules, adds no new package dependency, and introduces no database or frontend complexity.

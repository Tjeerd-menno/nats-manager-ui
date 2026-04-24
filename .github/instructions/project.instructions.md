---
description: "Use for any task in this repository. Provides project overview, architecture summary, and pointers to domain-specific instruction files."
applyTo: "**"
---
# NATS Admin UI — Project Instructions

## Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, ASP.NET Core Minimal APIs, EF Core (SQLite), NATS.Net v2 |
| Frontend | React 19, TypeScript (strict), Mantine 7, TanStack Query, Vite |
| Orchestration | .NET Aspire |
| Testing | xUnit v3 + MTP v2, Shouldly, NSubstitute, Vitest, React Testing Library, Playwright |

## Architecture

Clean Architecture with Ports & Adapters. Four backend layers:

- **Domain** — DDD aggregates, value objects, enumerations. No external dependencies.
- **Application** — Use cases (`IUseCase<TRequest, TResult>`), output ports (`IOutputPort<T>`), validators (FluentValidation). No framework references.
- **Infrastructure** — NATS adapters, EF Core repositories, auth services. Implements Application ports.
- **Web** — Minimal API endpoints, `Presenter<T>` (implements `IOutputPort<T>`), middleware, DI composition.

Frontend follows feature-folder organization under `src/features/`. Shared components live in `src/shared/`.

## Key Commands

```bash
# Run the full stack (backend + frontend + NATS)
aspire run

# Backend tests
dotnet test

# Frontend tests
cd src/NatsManager.Frontend && npm test

# Frontend lint
cd src/NatsManager.Frontend && npm run lint
```

## Module Structure

Both backend and frontend organize code by domain module: `Audit`, `Auth`, `CoreNats`, `Dashboard`, `Environments`, `JetStream`, `KeyValue`, `ObjectStore`, `Search`, `Services`.

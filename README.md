# NATS Admin UI

A unified, open-source web application for inspecting and administering [NATS](https://nats.io) environments — Core NATS, JetStream, Key-Value Store, Object Store, and NATS Services — through a single pane of glass.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-strict-3178C6?logo=typescript)](https://www.typescriptlang.org/)

> [!WARNING]
> **Alpha — active development.** NATS Admin UI is in early-stage development and is **not yet production-ready**. APIs, data models, and UI may change without notice between releases. Expect bugs and incomplete features. Feedback and contributions are very welcome!

---

## Overview

NATS Admin UI gives operators, platform engineers, and developers a consistent way to observe and manage every NATS capability area without juggling CLI tools or writing ad-hoc scripts. It is designed around three principles:

- **Clarity** — always show which environment is selected, whether data is live or stale, and which actions are destructive.
- **Safety** — destructive operations require explicit confirmation, are scoped by role, and are recorded in an audit log.
- **Consistency** — the same navigation and interaction patterns across Streams, Consumers, KV buckets, Object Store buckets, Subjects, and Services.

## Features

- 🌐 **Multi-environment management** — register and switch between multiple NATS clusters, with live connection/health indicators and staleness awareness.
- 📊 **Unified dashboard** — cross-resource health summary with drill-down into any area.
- 🧵 **JetStream** — browse streams and consumers; inspect configuration, state, backlog, and health; create/update/delete with safeguards; peek at stored messages.
- 🗝️ **Key-Value Store** — browse buckets and keys; inspect values, metadata, and revisions; distinguish current / deleted / superseded states; overwrite warnings on coordination data.
- 📦 **Object Store** — browse buckets and objects; upload, download, replace, and delete with size and impact warnings.
- 🛰️ **Services** — discover NATS micro services; inspect endpoints, versions, and health; send test requests with side-effect warnings.
- 📡 **Core NATS** — explore subjects, subscriptions, connected clients, and traffic; publish test messages and subscribe to live streams.
- 🔎 **Global search & bookmarks** — cross-resource search and filtering; bookmark frequently used resources.
- 🔐 **Authentication, RBAC & audit** — role-based access (ReadOnly / Operator / Administrator), environment-scoped policies, and a searchable audit log of all state-changing actions.
- ⚡ **Built for scale** — virtualized lists for 1k+ resources, bounded NATS timeouts, and sub-second search.

## Architecture

A modular monolith using Clean Architecture (Ports & Adapters):

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NatsManager.Domain` | DDD aggregates, value objects, enums. No external dependencies. |
| Application | `NatsManager.Application` | Use cases (commands/queries), output ports, FluentValidation validators. No framework references. |
| Infrastructure | `NatsManager.Infrastructure` | NATS adapters (NATS.Net v2), EF Core (SQLite) repositories, auth services. |
| Web | `NatsManager.Web` | ASP.NET Core Minimal API endpoints, presenters, middleware, DI composition. |
| Frontend | `NatsManager.Frontend` | React 19 + Mantine 7 SPA organized by feature folders. |
| Orchestration | `NatsManager.AppHost` / `NatsManager.ServiceDefaults` | .NET Aspire local orchestration, OpenTelemetry, health checks, resilience. |

**Bounded contexts** (mirrored on backend and frontend): `Audit`, `Auth`, `CoreNats`, `Dashboard`, `Environments`, `JetStream`, `KeyValue`, `ObjectStore`, `Search`, `Services`.

**Storage**
- **SQLite** — application data (environments, users, audit trail, bookmarks, preferences).
- **NATS** — live resource state (streams, consumers, KV, Object Store, services) is always read from the cluster.

**Deployment** — a single OCI container serves the API and the built SPA. Local development uses .NET Aspire to orchestrate NATS, backend, and the Vite dev server.

## Tech Stack

**Backend**
- .NET 10 · ASP.NET Core Minimal APIs
- EF Core 10 (SQLite)
- [NATS.Net](https://github.com/nats-io/nats.net) v2
- MediatR · FluentValidation · Serilog
- xUnit v3 + Microsoft Testing Platform, FluentAssertions, NSubstitute

**Frontend**
- React 19 · TypeScript (strict)
- Mantine 7 · Tabler Icons · Recharts
- TanStack Query · TanStack Virtual · React Router
- Vite · Vitest · React Testing Library · MSW

**Orchestration & Tooling**
- .NET Aspire (AppHost + ServiceDefaults)
- Playwright (E2E tests)
- ESLint · Prettier · `dotnet format`

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22 LTS](https://nodejs.org/) (with npm)
- [Docker](https://www.docker.com/) (for the Aspire-managed NATS container)
- .NET Aspire workload: `dotnet workload install aspire`

### Run the full stack with Aspire (recommended)

```bash
cd src/NatsManager.AppHost
dotnet run
```

This launches:

- A **NATS** container with JetStream enabled (`4222`)
- The **backend** ASP.NET Core API
- The **frontend** Vite dev server
- The **Aspire Dashboard** (logs, traces, metrics) — opens automatically

### Run components manually

<details>
<summary>Backend</summary>

```bash
cd src/NatsManager.Web
dotnet restore
dotnet ef database update --project ../NatsManager.Infrastructure
dotnet run
```

API available at `http://localhost:5000` (Swagger at `/swagger`, health at `/health`).
</details>

<details>
<summary>Frontend</summary>

```bash
cd src/NatsManager.Frontend
npm install
npm run dev
```

Vite dev server at `http://localhost:5173`.
</details>

<details>
<summary>NATS (standalone container)</summary>

```bash
docker run -d --name nats-dev \
  -p 4222:4222 -p 8222:8222 \
  nats:latest -js -m 8222
```
</details>

### First steps

1. Start the stack via Aspire.
2. Open the frontend URL shown in the Aspire Dashboard.
3. Sign in with the default admin account (created during the first migration).
4. Go to **Environments** and register a NATS server (e.g. `nats://localhost:4222`).
5. Test the connection — a green "Available" status indicates success.
6. Explore JetStream, KV, Object Store, Services, and Core NATS for that environment.

## Project Structure

```text
src/
├── NatsManager.AppHost/           # .NET Aspire orchestrator (dev only)
├── NatsManager.ServiceDefaults/   # Shared Aspire config (OTel, health, resilience)
├── NatsManager.Domain/            # DDD aggregates, value objects
├── NatsManager.Application/       # Use cases, ports, validators
├── NatsManager.Infrastructure/    # NATS adapters, EF Core repositories, auth
├── NatsManager.Web/               # Minimal API endpoints, DI, Dockerfile
└── NatsManager.Frontend/          # React 19 SPA (Vite)
    └── src/features/{audit,auth,core-nats,dashboard,environments,
                      jetstream,kv,objectstore,search,services}/

tests/
├── NatsManager.Domain.Tests/
├── NatsManager.Application.Tests/
├── NatsManager.Infrastructure.Tests/
├── NatsManager.Web.Tests/         # API + contract tests
├── NatsManager.Integration.Tests/
└── NatsManager.E2E.Tests/         # Playwright E2E

specs/                             # SpecKit feature artifacts
└── 001-nats-admin-app/
    ├── spec.md                    # Functional specification
    ├── plan.md                    # Implementation plan
    ├── data-model.md
    ├── contracts/
    ├── research.md
    ├── quickstart.md
    └── tasks.md
```

## Development

### Build

```bash
dotnet build
cd src/NatsManager.Frontend && npm run build
```

### Test

```bash
# Backend unit + integration tests
dotnet test

# Frontend unit tests
cd src/NatsManager.Frontend
npm test
npm run test:coverage
```

### Lint & format

```bash
dotnet format
cd src/NatsManager.Frontend && npm run lint
```

### Production container

```bash
docker build -t nats-admin:dev -f src/NatsManager.Web/Dockerfile .
docker run -d -p 8080:8080 -v nats-admin-data:/data nats-admin:dev
```

## Performance Targets

- First Contentful Paint ≤ 1.5s · Time to Interactive ≤ 2.0s
- API p95 ≤ 1s · 1k-item list render ≤ 200ms
- Initial JS bundle ≤ 300 KB gzipped
- Supports up to ~10,000 resources per environment

## Specifications

This project is built with a spec-first approach. Full specifications, architecture decisions, data model, API contracts, and task breakdown live in [`specs/001-nats-admin-app/`](./specs/001-nats-admin-app/). Higher-level documents:

- [`nats-management-functional-spec.md`](./nats-management-functional-spec.md)
- [`nats-management-technical-architecture.md`](./nats-management-technical-architecture.md)

## Contributing

Contributions are welcome! Please:

1. Open an issue to discuss significant changes before submitting a PR.
2. Follow the patterns documented in `.github/instructions/` for the layer you are touching.
3. Ensure tests and linters pass (`dotnet test`, `npm test`, `dotnet format`, `npm run lint`).
4. Keep changes scoped and include tests for new behavior.

## License

Released under the [MIT License](./LICENSE).

## Acknowledgements

- [NATS.io](https://nats.io) and the [NATS.Net](https://github.com/nats-io/nats.net) client team
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/) · [Mantine](https://mantine.dev/) · [TanStack](https://tanstack.com/)

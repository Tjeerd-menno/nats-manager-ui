# nats-admin-ui Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-25

## Active Technologies
- C# / .NET 10 (backend), TypeScript strict mode (frontend) + ASP.NET Core 10 (Minimal APIs), EF Core 10, NATS.Net v2, MediatR, FluentValidation, Serilog, React 19, Mantine 7, Recharts, TanStack Query, @tanstack/react-virtual, Vite, Vitest (001-nats-admin-app)
- SQLite (application data: environments, users, audit, bookmarks, preferences); NATS (live resource state) (001-nats-admin-app)
- C# / .NET 10 (backend), TypeScript strict mode (frontend) + ASP.NET Core 10 SignalR (built-in, no new NuGet), `@microsoft/signalr` (new npm dep), `System.Net.Http.HttpClient` (built-in), Recharts 3.8.1 (existing) (copilot/add-live-monitoring-feature)
- In-memory ring buffer only (no SQLite persistence for monitoring data). Two new nullable columns on existing `Environments` SQLite table via EF Core migration. (copilot/add-live-monitoring-feature)

- C# / .NET 10 (backend), TypeScript (frontend) + ASP.NET Core 10 (Minimal APIs), EF Core 10, NATS.Net (official NATS .NET client v2), React 19, Mantine 7, Recharts (001-nats-admin-app)

## Project Structure

```text
src/
  NatsManager.AppHost/           # .NET Aspire orchestration
  NatsManager.Application/       # Use cases, ports, validators
  NatsManager.Domain/            # DDD aggregates, value objects
  NatsManager.Frontend/          # React 19 + Mantine + Vite
  NatsManager.Infrastructure/    # NATS adapters, EF Core repos
  NatsManager.ServiceDefaults/   # Shared Aspire config
  NatsManager.Web/               # Minimal API endpoints, DI
tests/
  NatsManager.Application.Tests/
  NatsManager.Domain.Tests/
  NatsManager.E2E.Tests/
  NatsManager.Infrastructure.Tests/
  NatsManager.Integration.Tests/
  NatsManager.Web.Tests/
specs/                           # SpecKit feature artifacts
```

## Commands

npm test; npm run lint

## Code Style

C# / .NET 10 (backend), TypeScript (frontend): Follow standard conventions

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

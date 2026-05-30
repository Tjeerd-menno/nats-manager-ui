# nats-admin-ui Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-25

## Active Technologies
- C# / .NET 10 (backend), TypeScript strict mode (frontend) + ASP.NET Core 10 (Minimal APIs), EF Core 10, NATS.Net v2, FluentValidation, Serilog, React 19, Mantine 9, Recharts, TanStack Query, @tanstack/react-virtual, Vite, Vitest (001-nats-admin-app)
- SQLite (application data: environments, users, audit, bookmarks, preferences); NATS (live resource state) (001-nats-admin-app)
- C# / .NET 10 (backend), TypeScript strict mode (frontend) + ASP.NET Core 10 SignalR (built-in, no new NuGet), `@microsoft/signalr` (new npm dep), `System.Net.Http.HttpClient` (built-in), Recharts 3.8.1 (existing) (copilot/add-live-monitoring-feature)
- In-memory ring buffer only (no SQLite persistence for monitoring data). Two new nullable columns on existing `Environments` SQLite table via EF Core migration. (copilot/add-live-monitoring-feature)

- C# / .NET 10 (backend), TypeScript (frontend) + ASP.NET Core 10 (Minimal APIs), EF Core 10, NATS.Net (official NATS .NET client v2), React 19, Mantine 9, Recharts (001-nats-admin-app)

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

### Frontend
```bash
cd src/NatsManager.Frontend
npm test        # Vitest unit tests
npm run lint    # ESLint
```

### Backend — Build
```bash
# Solution file is NatsManager.slnx (Aspire format — no .sln exists)
dotnet build NatsManager.slnx -c Debug

# dotnet build only accepts ONE project path at a time (MSB1008 with multiple paths)
dotnet build src/NatsManager.Web/NatsManager.Web.csproj -c Debug
```

### Backend — Run Tests
Plain `dotnet test` from the repository root is not the supported all-backend test command with xUnit v3 + Microsoft Testing Platform v2. Run unit test projects explicitly (as CI does):
```bash
tests\NatsManager.Domain.Tests\bin\Debug\net10.0\NatsManager.Domain.Tests.exe
tests\NatsManager.Application.Tests\bin\Debug\net10.0\NatsManager.Application.Tests.exe
tests\NatsManager.Infrastructure.Tests\bin\Debug\net10.0\NatsManager.Infrastructure.Tests.exe
tests\NatsManager.Web.Tests\bin\Debug\net10.0\NatsManager.Web.Tests.exe
```

### Backend — Warnings-as-errors
`src/` projects have warnings-as-errors enabled; test projects do not. Common build-breaking warnings:
- **CA1859** — use `ConfigurationManager` (concrete type) instead of `IConfiguration` when the concrete type flows in
- **CA1725** — parameter names on overrides/implementations must exactly match the interface
- **Unused `using` directives** — remove after refactoring endpoint files

## Code Style

C# / .NET 10 (backend), TypeScript (frontend): Follow standard conventions

### File editing gotcha
The `create` tool **cannot overwrite existing files**. To replace an existing file from scratch, create a `.new` temp file then use `Move-Item -Force`. Use the `edit` tool for all partial edits to existing files.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->

---
description: "Use when working with NATS adapters, EF Core persistence, database migrations, or external service integration in the Infrastructure layer."
applyTo: "src/NatsManager.Infrastructure/**"
---
# Infrastructure Layer Instructions

## NATS Adapters

Each Application port interface has a corresponding adapter in `Infrastructure/Nats/`:

| Port (Application) | Adapter (Infrastructure) |
|---|---|
| `ICoreNatsAdapter` | `CoreNatsAdapter` |
| `IJetStreamAdapter` + `IJetStreamWriteAdapter` | `JetStreamAdapter` (implements both) |
| `IKvStoreAdapter` | `KvStoreAdapter` |
| `IObjectStoreAdapter` | `ObjectStoreAdapter` |
| `IServiceDiscoveryAdapter` | `ServiceDiscoveryAdapter` |

### Connection Management

- `NatsConnectionFactory` manages `NatsConnection` instances per environment ID
- `EnvironmentConnectionResolver` resolves environment config from the repository
- `NatsAuthHelper` handles credential types (None, Token, NKey, UserCredentials)

### Adapter Conventions

- Adapters are registered as **singletons** (connections are long-lived)
- Use `partial class` with `[LoggerMessage]` for structured logging
- Handle NATS exceptions gracefully (log + return null/empty, never throw for missing resources)
- Always accept `CancellationToken` on async methods

## EF Core Persistence

- **SQLite** database: `natsmanager.db`
- `AppDbContext` with entity configurations in `Persistence/Configurations/`
- Repositories are registered as **scoped** services
- `DatabaseInitializer` handles migrations + bootstrap admin user creation

### Repository Conventions

- Implement the interface from `Application/Modules/{Module}/Ports/`
- Support pagination with `PaginatedResult<T>` for list queries
- Use `AsNoTracking()` for read-only queries

## Registration Pattern

Infrastructure services are wired in `Web/Program.cs`:
- NATS adapters: `AddSingleton<IPort, Adapter>()`
- Repositories: `AddScoped<IRepository, Repository>()`
- Auth services: `AddSingleton<ICredentialEncryptionService, AesEncryptionService>()`

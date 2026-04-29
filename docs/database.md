# Database — provider selection and migrations

NATS Admin UI persists application data (environments, users, role assignments, audit events,
bookmarks, user preferences) through Entity Framework Core. As of v0.x it supports two providers:

| Provider | Default? | Use case |
|---|---|---|
| **SQLite** | ✅ | Local development, single-node deployments, demos. Zero-config. |
| **PostgreSQL** |   | Production, shared/HA deployments, environments where a managed RDBMS is preferred. |

Live NATS resource state (streams, consumers, KV, Object Store, services) is **not** stored in
the database; it is always read from the cluster.

## Configuration

The provider is selected by the `Database:Provider` configuration key (or
`Database__Provider` when supplied via environment variables). The connection string is read
from the standard `ConnectionStrings:DefaultConnection` key.

### SQLite (default)

No configuration is required. With no settings supplied the application creates / opens
`natsmanager.db` in the working directory. To customize the location:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/natsmanager/natsmanager.db"
  }
}
```

### PostgreSQL

```json
{
  "Database": {
    "Provider": "Postgres"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.internal;Port=5432;Database=natsmanager;Username=natsmanager;Password=<secret>"
  }
}
```

Equivalent environment variables (e.g. for Docker / Kubernetes):

```bash
Database__Provider=Postgres
ConnectionStrings__DefaultConnection=Host=db.internal;Port=5432;Database=natsmanager;Username=natsmanager;Password=...
```

Startup will fail fast with a clear error if `Database:Provider=Postgres` is set without a
connection string.

## Migrations

Each provider owns its own migration set; they cannot be reused across providers. They live
side-by-side under the Infrastructure project:

```
src/NatsManager.Infrastructure/Persistence/Migrations/
├── Sqlite/      ← SQLite migrations + ModelSnapshot (namespace ...Migrations.Sqlite)
└── Postgres/    ← PostgreSQL migrations + ModelSnapshot (namespace ...Migrations.Postgres)
```

At runtime, `ProviderScopedMigrationsAssembly` (registered in `AppDbContext.OnConfiguring`)
filters EF's migration discovery to the namespace matching the active provider, so each
provider only ever sees its own migrations and `__EFMigrationsHistory` table.

### Adding a new migration

When you make a domain or schema change you must generate **two** migrations — one per
provider — so both stay in lockstep:

```bash
# SQLite (default)
dotnet ef migrations add <Name> \
  --project src/NatsManager.Infrastructure \
  --startup-project src/NatsManager.Infrastructure \
  --output-dir Persistence/Migrations/Sqlite \
  --namespace NatsManager.Infrastructure.Persistence.Migrations.Sqlite \
  -- --provider Sqlite

# PostgreSQL
dotnet ef migrations add <Name> \
  --project src/NatsManager.Infrastructure \
  --startup-project src/NatsManager.Infrastructure \
  --output-dir Persistence/Migrations/Postgres \
  --namespace NatsManager.Infrastructure.Persistence.Migrations.Postgres \
  -- --provider Postgres
```

The `-- --provider <Sqlite|Postgres>` tail is forwarded to
`AppDbContextDesignTimeFactory.CreateDbContext(string[] args)` and selects the EF provider
used at design-time. You can also set `DESIGNTIME_PROVIDER=Postgres` if you prefer not to
pass it on every command. Use `DESIGNTIME_CONNECTION_STRING` to override the design-time
connection string (no real database connection is required to scaffold migrations).

### Removing the most recent migration

```bash
dotnet ef migrations remove \
  --project src/NatsManager.Infrastructure \
  --startup-project src/NatsManager.Infrastructure \
  -- --provider <Sqlite|Postgres>
```

### Generating SQL scripts

```bash
dotnet ef migrations script \
  --project src/NatsManager.Infrastructure \
  --startup-project src/NatsManager.Infrastructure \
  -- --provider Postgres
```

## Aspire integration

`aspire run` defaults to SQLite, requiring no Docker container for the database. To run the
full stack against a real PostgreSQL container managed by Aspire, set the
`DATABASE_PROVIDER` environment variable before launching:

```bash
DATABASE_PROVIDER=Postgres aspire run
```

The AppHost (`src/NatsManager.AppHost/AppHost.cs`) then provisions:

- A persistent PostgreSQL container with a data volume.
- The `natsmanager` database.
- The backend with `Database__Provider=Postgres` and the connection string injected.

## Provider-specific notes

### SQLite
- WAL journal mode is enabled at startup (`PRAGMA journal_mode=WAL`) for better concurrent
  read performance. This pragma is gated to SQLite only.
- Default text comparison is case-sensitive (BINARY collation). Username and Environment
  name unique indexes therefore behave identically to PostgreSQL.

### PostgreSQL
- `DateTimeOffset` columns are persisted as `character varying(48)` (ISO 8601 string).
  Npgsql does not have a native PostgreSQL type for `DateTimeOffset` because PostgreSQL has
  no equivalent of "instant + offset". All `DateTimeOffset` values produced by the
  application use `DateTimeOffset.UtcNow`, so equality and range comparisons work correctly
  given the canonical ISO 8601 format. If query performance on these columns becomes a
  concern, the migration can be amended to use `timestamp with time zone` plus a value
  converter — this is intentionally **not** done for the initial release to keep the schema
  simple and the domain model unchanged.
- Identifiers (table and column names) preserve PascalCase via Npgsql's automatic quoting.
  No raw SQL in the code base depends on lowercase identifiers.
- Default text comparison is case-sensitive — same as SQLite's default — so unique indexes
  on `Users.Username` and `Environments.Name` behave identically across providers. Username
  and environment name normalization remains the responsibility of the domain layer.

## Out of scope

- **Live data migration from SQLite to PostgreSQL.** Switching providers means starting from
  an empty PostgreSQL database; export/import tooling is not provided.
- **Switching providers at runtime.** The provider is selected once at startup.
- **Multi-tenant / sharded PostgreSQL.**

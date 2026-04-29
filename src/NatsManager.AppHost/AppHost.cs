var builder = DistributedApplication.CreateBuilder(args);

var bootstrapAdminUsername = builder.AddParameter("bootstrap-admin-username")
    .WithDescription("Bootstrap admin username used only when the user store is empty.");
var bootstrapAdminPassword = builder.AddParameter("bootstrap-admin-password", secret: true)
    .WithDescription("Bootstrap admin password used only for first-run initialization.");
var encryptionKey = builder.AddParameter("backend-encryption-key", secret: true)
    .WithDescription("Base64-encoded 32-byte encryption key for stored credentials.");

var nats = builder.AddNats("nats")
    .WithArgs("-js", "-m", "8222")
    .WithEndpoint(targetPort: 8222, name: "monitoring", scheme: "http")
    .WithLifetime(ContainerLifetime.Persistent);

var backend = builder.AddProject<Projects.NatsManager_Web>("backend")
    .WithReference(nats)
    .WithEnvironment("BootstrapAdmin__Username", bootstrapAdminUsername)
    .WithEnvironment("BootstrapAdmin__Password", bootstrapAdminPassword)
    .WithEnvironment("Encryption__Key", encryptionKey)
    .WithEnvironment("CoreNats__Monitoring__BaseUrl", nats.GetEndpoint("monitoring"))
    .WaitFor(nats);

// Optional PostgreSQL provider — opt-in via the DATABASE_PROVIDER environment variable
// (set to "Postgres") so that the default `aspire run` experience remains zero-config SQLite.
// When enabled, Aspire spins up a Postgres container, creates the `natsmanager` database, and
// injects the connection string as `ConnectionStrings__natsmanager`. The backend is configured
// to read it from `ConnectionStrings__DefaultConnection` and to use the Postgres provider.
var databaseProvider = builder.Configuration["DATABASE_PROVIDER"]
    ?? Environment.GetEnvironmentVariable("DATABASE_PROVIDER");

if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
{
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);
    var natsManagerDb = postgres.AddDatabase("natsmanager");

    backend
        .WithReference(natsManagerDb)
        .WithEnvironment("Database__Provider", "Postgres")
        .WithEnvironment("ConnectionStrings__DefaultConnection", natsManagerDb)
        .WaitFor(natsManagerDb);
}

builder.AddViteApp("frontend", "../NatsManager.Frontend", "dev")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();

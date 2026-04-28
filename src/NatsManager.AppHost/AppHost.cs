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

builder.AddViteApp("frontend", "../NatsManager.Frontend", "dev")
    .WithReference(backend)
    .WaitFor(backend);

builder.Build().Run();

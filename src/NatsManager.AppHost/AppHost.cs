var builder = DistributedApplication.CreateBuilder(args);

var bootstrapAdminUsername = builder.AddParameter("bootstrap-admin-username")
    .WithDescription("Bootstrap admin username used only when the user store is empty.");
var bootstrapAdminPassword = builder.AddParameter("bootstrap-admin-password", secret: true)
    .WithDescription("Bootstrap admin password used only for first-run initialization.");
var natsUsername = builder.AddParameter("nats-username")
    .WithDescription("Username for the Aspire-managed local NATS server.");
var natsPassword = builder.AddParameter("nats-password", secret: true)
    .WithDescription("Password for the Aspire-managed local NATS server.");
var encryptionKey = builder.AddParameter("backend-encryption-key", secret: true)
    .WithDescription("Base64-encoded 32-byte encryption key for stored credentials.");

var openIdentityStackEnabled = !string.Equals(
    builder.Configuration["OPENIDENTITYSTACK_ENABLED"] ?? Environment.GetEnvironmentVariable("OPENIDENTITYSTACK_ENABLED"),
    "false",
    StringComparison.OrdinalIgnoreCase);
var openIdentityStackImageTag = builder.Configuration["OPENIDENTITYSTACK_IMAGE_TAG"]
    ?? Environment.GetEnvironmentVariable("OPENIDENTITYSTACK_IMAGE_TAG")
    ?? "v0.1.3";

var nats = builder.AddNats("nats", userName: natsUsername, password: natsPassword)
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
// When enabled, Aspire spins up a Postgres container and creates the `natsmanager` database.
// This AppHost then passes that database connection to the backend via
// `ConnectionStrings__DefaultConnection` and sets `Database__Provider` to `Postgres`.
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

var frontend = builder.AddViteApp("frontend", "../NatsManager.Frontend", "dev")
    .WithReference(backend)
    .WaitFor(backend);

if (openIdentityStackEnabled)
{
    var openIdentityStackAdminPassword = builder.AddParameter("openidentitystack-admin-password", secret: true)
        .WithDescription("Password for the OpenIdentityStack seeded development admin user (admin@localhost.dev).");

    var openIdentityPostgres = builder.AddPostgres("openidentitystack-postgres")
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent);
    var openIdentityDb = openIdentityPostgres.AddDatabase("openidentitystack");

    var openIdentityApi = builder.AddContainer(
            "openidentitystack-api",
            "ghcr.io/tjeerd-menno/open-identity-stack-api",
            openIdentityStackImageTag)
        .WithReference(openIdentityDb)
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WaitFor(openIdentityDb);
    var openIdentityAuthority = openIdentityApi.GetEndpoint("http");
    openIdentityApi.WithEnvironment("OpenIddict__Issuer", openIdentityAuthority);

    var frontendHttp = frontend.GetEndpoint("http");
    var oidcCallbackUri = ReferenceExpression.Create($"{frontendHttp}/signin-oidc");
    var postLogoutUri = ReferenceExpression.Create($"{frontendHttp}/login");

    var openIdentityMigrator = builder.AddContainer(
            "openidentitystack-db-migrator",
            "ghcr.io/tjeerd-menno/open-identity-stack-db-migrator",
            openIdentityStackImageTag)
        .WithReference(openIdentityDb)
        .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
        .WithEnvironment("OpenIddict__Issuer", openIdentityAuthority)
        .WithEnvironment("Seed__DevelopmentData", "true")
        .WithEnvironment("Seed__DefaultAdmin__Password", openIdentityStackAdminPassword)
        .WithEnvironment("OpenIddict__Clients__AdminWeb__RedirectUris__0", oidcCallbackUri)
        .WithEnvironment("OpenIddict__Clients__AdminWeb__PostLogoutRedirectUris__0", frontendHttp)
        .WithEnvironment("OpenIddict__Clients__AdminWeb__PostLogoutRedirectUris__1", postLogoutUri)
        .WaitFor(openIdentityDb);

    openIdentityApi.WaitForCompletion(openIdentityMigrator);

    builder.AddContainer(
            "openidentitystack-adminweb",
            "ghcr.io/tjeerd-menno/open-identity-stack-admin-web",
            openIdentityStackImageTag)
        .WithHttpEndpoint(targetPort: 8080, name: "http")
        .WithEnvironment("VITE_OIDC_AUTHORITY", openIdentityAuthority)
        .WithEnvironment("VITE_API_BASE_URL", openIdentityAuthority)
        .WaitFor(openIdentityApi);

    backend
        .WithEnvironment("Oidc__Enabled", "true")
        .WithEnvironment("Oidc__Authority", openIdentityAuthority)
        .WithEnvironment("Oidc__ClientId", "admin-web-client")
        .WithEnvironment("Oidc__RequireHttpsMetadata", "false")
        .WithEnvironment("Oidc__PublicOrigin", frontendHttp)
        .WithEnvironment("Oidc__PostLogoutRedirectUri", postLogoutUri)
        .WithEnvironment("Oidc__AllowedRedirectOrigins__0", frontendHttp)
        .WithEnvironment("Oidc__DefaultRoles__0", "Administrator")
        .WaitFor(openIdentityApi);
}

builder.Build().Run();

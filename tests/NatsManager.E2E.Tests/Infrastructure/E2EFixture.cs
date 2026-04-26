using Aspire.Hosting;
using Aspire.Hosting.Testing;
using NatsManager.E2E.Tests.Infrastructure;
using System.Text;

#pragma warning disable CS8602 // Aspire configureBuilder parameters are non-null at runtime

[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: AssemblyFixture(typeof(AppHostFixture))]

namespace NatsManager.E2E.Tests.Infrastructure;

/// <summary>
/// Assembly-level fixture that starts the Aspire distributed application once for all E2E tests.
/// Provides URLs for the backend API and frontend dev server.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan ResourceTimeout = TimeSpan.FromMinutes(5);
    private const string UsernameParameter = "Parameters__bootstrap-admin-username";
    private const string PasswordParameter = "Parameters__bootstrap-admin-password";
    private const string EncryptionKeyParameter = "Parameters__backend-encryption-key";
    public const string BootstrapAdminUsername = "admin";
    public const string BootstrapAdminPassword = "Admin123!";
    public static string EncryptionKey { get; } =
        Convert.ToBase64String(Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"));

    private DistributedApplication app = default!;
    private ResourceNotificationService resourceNotificationService = default!;
    private string? _dbPath;
    private string? _originalUsernameParameter;
    private string? _originalPasswordParameter;
    private string? _originalEncryptionKeyParameter;

    public string FrontendUrl { get; private set; } = string.Empty;
    public string BackendUrl { get; private set; } = string.Empty;
    public string NatsUrl { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Use an ephemeral SQLite database so each test run starts clean
        _dbPath = Path.Combine(Path.GetTempPath(), $"natsmanager-e2e-{Guid.NewGuid():N}.db");

        _originalUsernameParameter = Environment.GetEnvironmentVariable(UsernameParameter);
        _originalPasswordParameter = Environment.GetEnvironmentVariable(PasswordParameter);
        _originalEncryptionKeyParameter = Environment.GetEnvironmentVariable(EncryptionKeyParameter);

        Environment.SetEnvironmentVariable(UsernameParameter, BootstrapAdminUsername);
        Environment.SetEnvironmentVariable(PasswordParameter, BootstrapAdminPassword);
        Environment.SetEnvironmentVariable(EncryptionKeyParameter, EncryptionKey);

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.NatsManager_AppHost>(
                args: [],
                configureBuilder: (appOptions, _) => appOptions.DisableDashboard = true);

        // Override the NATS resource to use session lifetime (not persistent)
        // so each test run gets a fresh NATS server
        var natsResource = appHost.Resources.Single(r => r.Name == "nats");
        var lifetimeAnnotation = natsResource.Annotations.OfType<ContainerLifetimeAnnotation>().FirstOrDefault();
        if (lifetimeAnnotation is not null)
        {
            natsResource.Annotations.Remove(lifetimeAnnotation);
        }

        // Inject ephemeral SQLite path into the backend project resource
        var backendResource = appHost.Resources.Single(r => r.Name == "backend");
        backendResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ConnectionStrings__DefaultConnection"] = $"Data Source={_dbPath}";
            context.EnvironmentVariables["BootstrapAdmin__Username"] = BootstrapAdminUsername;
            context.EnvironmentVariables["BootstrapAdmin__Password"] = BootstrapAdminPassword;
            context.EnvironmentVariables["Encryption__Key"] = EncryptionKey;
            // Disable rate limiting and antiforgery in E2E test runs (see Program.cs guards).
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Testing";
        }));

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        this.app = await appHost.BuildAsync();
        this.resourceNotificationService = this.app.Services.GetRequiredService<ResourceNotificationService>();
        await this.app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceTimeout);

        // Wait for backend to be healthy
        await this.resourceNotificationService
            .WaitForResourceHealthyAsync("backend", cts.Token);

        // Wait for frontend to reach Running state (npm apps don't have health checks)
        await this.resourceNotificationService
            .WaitForResourceAsync("frontend", KnownResourceStates.Running, cts.Token);

        // Resolve endpoint URLs
        using var backendClient = this.app.CreateHttpClient("backend");
        BackendUrl = backendClient.BaseAddress?.ToString().TrimEnd('/')
            ?? throw new InvalidOperationException("Backend URL not found.");

        using var frontendClient = this.app.CreateHttpClient("frontend");
        FrontendUrl = frontendClient.BaseAddress?.ToString().TrimEnd('/')
            ?? throw new InvalidOperationException("Frontend URL not found.");

        // Resolve NATS connection URL for test environment registration
        var natsConnectionString = await this.app.GetConnectionStringAsync("nats")
            ?? throw new InvalidOperationException("NATS connection string not found.");
        NatsUrl = natsConnectionString;

        // Poll until the Vite dev server is actually serving content
        await WaitForFrontendReadyAsync(cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.app is not null)
        {
            await this.app.DisposeAsync();
        }

        // Clean up the ephemeral SQLite database
        if (_dbPath is not null && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
            try { File.Delete(_dbPath + "-wal"); } catch { /* best effort */ }
            try { File.Delete(_dbPath + "-shm"); } catch { /* best effort */ }
        }

        Environment.SetEnvironmentVariable(UsernameParameter, _originalUsernameParameter);
        Environment.SetEnvironmentVariable(PasswordParameter, _originalPasswordParameter);
        Environment.SetEnvironmentVariable(EncryptionKeyParameter, _originalEncryptionKeyParameter);

        GC.SuppressFinalize(this);
    }

    private async Task WaitForFrontendReadyAsync(CancellationToken ct)
    {
        using var handler = new System.Net.Http.HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        using var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var response = await httpClient.GetAsync(FrontendUrl, ct);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Vite dev server not ready yet
            }

            await Task.Delay(500, ct);
        }
    }
}

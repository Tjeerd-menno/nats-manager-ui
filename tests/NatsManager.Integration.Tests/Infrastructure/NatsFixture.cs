using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using NatsManager.Integration.Tests.Infrastructure;

[assembly: AssemblyFixture(typeof(NatsFixture))]

namespace NatsManager.Integration.Tests.Infrastructure;

/// <summary>
/// Assembly-level fixture that starts the Aspire AppHost and waits for the NATS resource.
/// Provides a connection string for integration tests.
/// </summary>
public sealed class NatsFixture : IAsyncLifetime
{
    private DistributedApplication _app = default!;

    public string NatsUrl { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("Parameters__bootstrap-admin-username", "admin");
        Environment.SetEnvironmentVariable("Parameters__bootstrap-admin-password", "Admin123!");
        Environment.SetEnvironmentVariable(
            "Parameters__backend-encryption-key",
            Convert.ToBase64String("0123456789ABCDEF0123456789ABCDEF"u8.ToArray()));

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.NatsManager_AppHost>(
                args: [],
                configureBuilder: (appOptions, _) => appOptions.DisableDashboard = true);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        _app = await appHost.BuildAsync();
        var resourceNotificationService = _app.Services.GetRequiredService<ResourceNotificationService>();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await resourceNotificationService.WaitForResourceHealthyAsync("nats", cts.Token);

        var connectionString = await _app.GetConnectionStringAsync("nats", cts.Token);
        NatsUrl = connectionString ?? throw new InvalidOperationException("NATS connection string not found.");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}

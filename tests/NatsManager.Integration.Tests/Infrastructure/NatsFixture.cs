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
    private const string UsernameParameter = "Parameters__bootstrap-admin-username";
    private const string PasswordParameter = "Parameters__bootstrap-admin-password";
    private const string EncryptionKeyParameter = "Parameters__backend-encryption-key";
    private const string TestOnlyEncryptionKey = "JFar2auhLPoLfMvwy62dhRltrwY3EEPmFJ1svc17pn0=";

    private DistributedApplication _app = default!;
    private string? _originalUsernameParameter;
    private string? _originalPasswordParameter;
    private string? _originalEncryptionKeyParameter;

    public string NatsUrl { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        _originalUsernameParameter = Environment.GetEnvironmentVariable(UsernameParameter);
        _originalPasswordParameter = Environment.GetEnvironmentVariable(PasswordParameter);
        _originalEncryptionKeyParameter = Environment.GetEnvironmentVariable(EncryptionKeyParameter);

        Environment.SetEnvironmentVariable(UsernameParameter, "admin");
        Environment.SetEnvironmentVariable(PasswordParameter, "Admin123!");
        Environment.SetEnvironmentVariable(EncryptionKeyParameter, TestOnlyEncryptionKey);

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

        Environment.SetEnvironmentVariable(UsernameParameter, _originalUsernameParameter);
        Environment.SetEnvironmentVariable(PasswordParameter, _originalPasswordParameter);
        Environment.SetEnvironmentVariable(EncryptionKeyParameter, _originalEncryptionKeyParameter);

        GC.SuppressFinalize(this);
    }
}

using Microsoft.Extensions.Options;
using NatsManager.Infrastructure.Configuration;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class CoreNatsAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private CoreNatsAdapter CreateAdapter()
    {
        var options = Options.Create(new CoreNatsMonitoringOptions());
        var factory = new DefaultHttpClientFactory();
        return new CoreNatsAdapter(ConnectionFactory, factory, options, NullLogger<CoreNatsAdapter>());
    }

    [Fact]
    public async Task GetServerInfoAsync_ShouldReturnServerInfo()
    {
        var adapter = CreateAdapter();

        var info = await adapter.GetServerInfoAsync(EnvironmentId);

        info.ShouldNotBeNull();
        info!.Version.ShouldNotBeNullOrEmpty();
        info.Port.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotThrow()
    {
        var adapter = CreateAdapter();

        var act = () => adapter.PublishAsync(EnvironmentId, "test.subject", "hello"u8.ToArray());

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task PublishAsync_WithHeaders_ShouldNotThrow()
    {
        var adapter = CreateAdapter();
        var headers = new Dictionary<string, string> { ["X-Test"] = "value" };

        var act = () => adapter.PublishAsync(EnvironmentId, "test.subject", "hello"u8.ToArray(), headers);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task ListSubjectsAsync_ShouldReturnResult()
    {
        var adapter = CreateAdapter();

        var result = await adapter.ListSubjectsAsync(EnvironmentId);

        result.ShouldNotBeNull();
        // Monitoring may or may not be available in test environment
        // but result should never be null
    }

    [Fact]
    public async Task ListClientsAsync_ShouldReturnList()
    {
        var adapter = CreateAdapter();

        var clients = await adapter.ListClientsAsync(EnvironmentId);

        clients.ShouldNotBeNull();
    }
}

/// <summary>Simple IHttpClientFactory that always creates a new HttpClient.</summary>
internal sealed class DefaultHttpClientFactory : IHttpClientFactory
{
    public System.Net.Http.HttpClient CreateClient(string name) => new();
}

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
    public async Task SubscribeAsync_WhenMessagePublished_YieldsMessage()
    {
        var adapter = CreateAdapter();
        var subject = $"test.subscribe.{Guid.NewGuid():N}";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var enumerator = adapter.SubscribeAsync(EnvironmentId, subject, cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var next = enumerator.MoveNextAsync().AsTask();
        await Task.Delay(250, cts.Token);
        await adapter.PublishAsync(EnvironmentId, subject, "hello"u8.ToArray(), cancellationToken: cts.Token);

        var yielded = await next.WaitAsync(cts.Token);

        yielded.ShouldBeTrue();
        enumerator.Current.Subject.ShouldBe(subject);
        enumerator.Current.PayloadBase64.ShouldBe(Convert.ToBase64String("hello"u8.ToArray()));
        enumerator.Current.IsBinary.ShouldBeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_WhenCancelled_StopsEnumeration()
    {
        var adapter = CreateAdapter();
        var subject = $"test.cancel.{Guid.NewGuid():N}";
        using var cts = new CancellationTokenSource();
        await using var enumerator = adapter.SubscribeAsync(EnvironmentId, subject, cts.Token)
            .GetAsyncEnumerator(cts.Token);

        var next = enumerator.MoveNextAsync().AsTask();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await next.WaitAsync(TimeSpan.FromSeconds(5)));
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

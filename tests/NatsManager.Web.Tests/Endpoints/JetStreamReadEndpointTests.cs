using System.Net;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.JetStream.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class JetStreamReadEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public JetStreamReadEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStreams_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.JetStreamAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<StreamInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/jetstream/streams");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetStreamDetail_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        var stream = new StreamInfo("orders", "Orders stream", ["orders.>"], "Limits", "File",
            100, 1024, 1, DateTimeOffset.UtcNow,
            new StreamState(100, 1024, null, null, 1, 100));
        var config = new StreamConfig("orders", "Orders stream", ["orders.>"], "Limits", -1, -1, -1, "File", 1, "Old", -1, false, false, false);
        _factory.JetStreamAdapter.GetStreamAsync(envId, "orders", Arg.Any<CancellationToken>())
            .Returns(stream);
        _factory.JetStreamAdapter.GetStreamConfigAsync(envId, "orders", Arg.Any<CancellationToken>())
            .Returns(config);
        _factory.JetStreamAdapter.ListConsumersAsync(envId, "orders", Arg.Any<CancellationToken>())
            .Returns(new List<ConsumerInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/jetstream/streams/orders");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumers_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.JetStreamAdapter.ListConsumersAsync(envId, "orders", Arg.Any<CancellationToken>())
            .Returns(new List<ConsumerInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/jetstream/streams/orders/consumers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConsumers_WithInvalidPagination_ShouldReturn422()
    {
        var envId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{envId}/jetstream/streams/orders/consumers?page=0&pageSize=0");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}

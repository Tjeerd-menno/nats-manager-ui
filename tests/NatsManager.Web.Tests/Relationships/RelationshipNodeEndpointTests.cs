using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.Relationships.Models;
using NSubstitute;
using Shouldly;

namespace NatsManager.Web.Tests.Relationships;

public sealed class RelationshipNodeEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public RelationshipNodeEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRelationshipNode_WhenNodeExists_ShouldReturnNodeDetail()
    {
        var environmentId = Guid.NewGuid();
        var stream = new StreamInfo(
            "orders",
            "Orders stream",
            ["orders.*"],
            "Limits",
            "File",
            10,
            1024,
            0,
            DateTimeOffset.UtcNow,
            new StreamState(10, 1024, null, null, 1, 10));
        _factory.JetStreamAdapter.GetStreamAsync(environmentId, "orders", Arg.Any<CancellationToken>())
            .Returns(stream);
        _factory.JetStreamAdapter.ListStreamsAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns([stream]);
        _factory.JetStreamAdapter.ListConsumersAsync(environmentId, "orders", Arg.Any<CancellationToken>())
            .Returns([]);
        var nodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Stream, "orders");

        var response = await _client.GetAsync($"/api/environments/{environmentId}/relationships/nodes/{Uri.EscapeDataString(nodeId)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nodeId").GetString().ShouldBe(nodeId);
        body.GetProperty("resourceType").GetString().ShouldBe("Stream");
        body.GetProperty("resourceId").GetString().ShouldBe("orders");
        body.GetProperty("displayName").GetString().ShouldBe("orders");
        body.GetProperty("status").GetString().ShouldBe("Healthy");
        body.GetProperty("freshness").GetString().ShouldBe("Live");
        body.GetProperty("detailRoute").GetString().ShouldBe("/jetstream/streams/orders");
        body.GetProperty("canRecenter").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task GetRelationshipNode_WhenNodeIsFromAnotherEnvironment_ShouldReturn404()
    {
        var environmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();
        var nodeId = ResourceNode.BuildNodeId(otherEnvironmentId, ResourceType.Stream, "orders");

        var response = await _client.GetAsync($"/api/environments/{environmentId}/relationships/nodes/{Uri.EscapeDataString(nodeId)}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRelationshipNode_WhenNodeIdIsInvalid_ShouldReturn400()
    {
        var environmentId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/environments/{environmentId}/relationships/nodes/{environmentId}:stream");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}

using System.Net;
using System.Net.Http.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.CoreNats.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class CoreNatsEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public CoreNatsEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetStatus_WhenNotFound_ShouldReturn404()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.GetServerInfoAsync(envId, Arg.Any<CancellationToken>())
            .Returns((NatsServerInfo?)null);

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/status");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSubjects_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<NatsSubjectInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/core-nats/subjects");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishMessage_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.CoreNatsAdapter.PublishAsync(envId, "test.subject", Arg.Any<byte[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var payload = new { Subject = "test.subject", Payload = "hello" };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/core-nats/publish", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

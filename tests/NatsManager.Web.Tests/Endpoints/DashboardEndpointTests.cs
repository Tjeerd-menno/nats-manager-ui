using System.Net;
using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Modules.Dashboard.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class DashboardEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public DashboardEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboard_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        var env = NatsManager.Domain.Modules.Environments.Environment.Create("Test", "nats://localhost:4222");
        _factory.EnvironmentRepository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _factory.JetStreamAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<NatsManager.Application.Modules.JetStream.Models.StreamInfo>());
        _factory.KvStoreAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<NatsManager.Application.Modules.KeyValue.Models.KvBucketInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/monitoring/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

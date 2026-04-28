using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSubstitute;
using Shouldly;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Monitoring.ClusterObservability;

public sealed class ClusterOverviewEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ClusterOverviewEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static ClusterObservation BuildObservation(Guid environmentId) => new(
        EnvironmentId: environmentId,
        ObservedAt: DateTimeOffset.UtcNow,
        Status: ClusterStatus.Healthy,
        Freshness: ObservationFreshness.Live,
        ServerCount: 2,
        DegradedServerCount: 0,
        JetStreamAvailable: true,
        ConnectionCount: 42,
        InMsgsPerSecond: 100.0,
        OutMsgsPerSecond: 50.0,
        Warnings: [],
        Servers: [],
        Topology: []);

    [Fact]
    public async Task GetClusterOverview_WhenEnvironmentNotFound_Returns404()
    {
        var envId = Guid.NewGuid();
        _factory.EnvironmentRepository
            .GetByIdAsync(envId, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var response = await _client.GetAsync($"/api/environments/{envId}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClusterOverview_WhenMonitoringNotConfigured_Returns400()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        // No monitoring URL configured
        _factory.EnvironmentRepository
            .GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);

        var response = await _client.GetAsync($"/api/environments/{env.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClusterOverview_WhenNoObservationData_Returns503()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository
            .GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);
        _factory.ClusterObservationStore
            .GetLatest(env.Id)
            .Returns((ClusterObservation?)null);

        var response = await _client.GetAsync($"/api/environments/{env.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetClusterOverview_WhenObservationAvailable_Returns200WithData()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateMonitoringSettings("http://localhost:8222", 30);
        var obs = BuildObservation(env.Id);

        _factory.EnvironmentRepository
            .GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);
        _factory.ClusterObservationStore
            .GetLatest(env.Id)
            .Returns(obs);

        var response = await _client.GetAsync($"/api/environments/{env.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClusterObservation>(JsonOptions);
        body.ShouldNotBeNull();
        body!.EnvironmentId.ShouldBe(env.Id);
        body.Status.ShouldBe(ClusterStatus.Healthy);
        body.ServerCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetClusterOverview_WhenClusterDegraded_Returns200WithDegradedStatus()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateMonitoringSettings("http://localhost:8222", 30);
        var obs = BuildObservation(env.Id) with
        {
            Status = ClusterStatus.Degraded,
            DegradedServerCount = 1,
            Warnings =
            [
                new ClusterWarning(
                    Code: "ServerUnavailable",
                    Severity: "Warning",
                    Message: "server-2 is unavailable",
                    ServerId: "server-2")
            ]
        };

        _factory.EnvironmentRepository
            .GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);
        _factory.ClusterObservationStore
            .GetLatest(env.Id)
            .Returns(obs);

        var response = await _client.GetAsync($"/api/environments/{env.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ClusterObservation>(JsonOptions);
        body!.Status.ShouldBe(ClusterStatus.Degraded);
        body.Warnings.Count.ShouldBe(1);
    }
}

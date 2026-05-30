using System.Net;
using NSubstitute;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Domain.Modules.Auth;
using Shouldly;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class MonitoringEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public MonitoringEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMonitoringHistory_WhenEnvironmentDoesNotExist_ShouldReturn404()
    {
        var environmentId = Guid.NewGuid();
        _factory.EnvironmentRepository.GetByIdAsync(environmentId, Arg.Any<CancellationToken>())
            .Returns((Environment?)null);

        var response = await _client.GetAsync($"/api/environments/{environmentId}/monitoring/metrics/history");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMonitoringHistory_WhenMonitoringIsNotConfigured_ShouldReturn400()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/metrics/history");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMonitoringHistory_WhenMonitoringIsConfigured_ShouldReturn200()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        var snapshot = new MonitoringSnapshot(
            environment.Id,
            DateTimeOffset.UtcNow,
            new ServerMetrics("", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            null,
            MonitoringStatus.Ok,
            MonitoringStatus.Ok);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.MonitoringMetricsStore.GetHistory(environment.Id).Returns([snapshot]);

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/metrics/history");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMonitoringHistory_WithScopedReadRole_ShouldReturn200()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.MonitoringMetricsStore.GetHistory(environment.Id).Returns([]);
        var client = _factory.CreateAuthenticatedClientWithScopedRole(Role.PredefinedNames.ReadOnly, environment.Id);

        var response = await client.GetAsync($"/api/environments/{environment.Id}/monitoring/metrics/history");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMonitoringHistory_WithNoAccessToEnvironment_ShouldReturn403()
    {
        var environmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();
        var client = _factory.CreateAuthenticatedClientWithScopedRole(Role.PredefinedNames.ReadOnly, otherEnvironmentId);

        var response = await client.GetAsync($"/api/environments/{environmentId}/monitoring/metrics/history");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetClusterOverview_WhenNoObservationData_ShouldReturn503()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.ClusterObservationStore.GetLatest(environment.Id).Returns((ClusterObservation?)null);

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetClusterOverview_WhenObservationAvailable_ShouldReturn200()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.ClusterObservationStore.GetLatest(environment.Id).Returns(BuildObservation(environment.Id));

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/cluster/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetClusterTopology_WhenNoObservationData_ShouldReturn503()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.ClusterObservationStore.GetLatest(environment.Id).Returns((ClusterObservation?)null);

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/cluster/topology");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GetClusterTopology_WhenObservationAvailable_ShouldReturn200()
    {
        var environment = Environment.Create("Test", "nats://localhost:4222");
        environment.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.GetByIdAsync(environment.Id, Arg.Any<CancellationToken>())
            .Returns(environment);
        _factory.ClusterObservationStore.GetLatest(environment.Id).Returns(BuildObservation(environment.Id));

        var response = await _client.GetAsync($"/api/environments/{environment.Id}/monitoring/cluster/topology");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static ClusterObservation BuildObservation(Guid environmentId) => new(
        EnvironmentId: environmentId,
        ObservedAt: DateTimeOffset.UtcNow,
        Status: ClusterStatus.Healthy,
        Freshness: ObservationFreshness.Live,
        ServerCount: 1,
        DegradedServerCount: 0,
        JetStreamAvailable: true,
        ConnectionCount: 5,
        InMsgsPerSecond: 10.0,
        OutMsgsPerSecond: 5.0,
        Warnings: [],
        Servers: [],
        Topology: []);
}

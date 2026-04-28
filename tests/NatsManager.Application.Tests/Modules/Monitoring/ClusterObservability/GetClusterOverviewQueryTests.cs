using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

namespace NatsManager.Application.Tests.Modules.Monitoring.ClusterObservability;

public sealed class GetClusterOverviewQueryTests
{
    private readonly IClusterObservationStore _store = Substitute.For<IClusterObservationStore>();
    private readonly GetClusterOverviewQueryHandler _handler;

    public GetClusterOverviewQueryTests()
    {
        _handler = new GetClusterOverviewQueryHandler(_store);
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
    public void Handle_WhenStoreHasData_ReturnsLatestObservation()
    {
        var envId = Guid.NewGuid();
        var obs = BuildObservation(envId);
        _store.GetLatest(envId).Returns(obs);

        var result = _handler.Handle(new GetClusterOverviewQuery(envId));

        result.ShouldNotBeNull();
        result!.EnvironmentId.ShouldBe(envId);
        result.Status.ShouldBe(ClusterStatus.Healthy);
        result.ServerCount.ShouldBe(2);
    }

    [Fact]
    public void Handle_WhenStoreEmpty_ReturnsNull()
    {
        var envId = Guid.NewGuid();
        _store.GetLatest(envId).Returns((ClusterObservation?)null);

        var result = _handler.Handle(new GetClusterOverviewQuery(envId));

        result.ShouldBeNull();
    }

    [Fact]
    public void Handle_EnvironmentIsolation_ReturnsCorrectEnvironmentData()
    {
        var envId1 = Guid.NewGuid();
        var envId2 = Guid.NewGuid();
        var obs1 = BuildObservation(envId1);
        var obs2 = BuildObservation(envId2) with { Status = ClusterStatus.Degraded };

        _store.GetLatest(envId1).Returns(obs1);
        _store.GetLatest(envId2).Returns(obs2);

        var result1 = _handler.Handle(new GetClusterOverviewQuery(envId1));
        var result2 = _handler.Handle(new GetClusterOverviewQuery(envId2));

        result1!.EnvironmentId.ShouldBe(envId1);
        result1.Status.ShouldBe(ClusterStatus.Healthy);
        result2!.EnvironmentId.ShouldBe(envId2);
        result2.Status.ShouldBe(ClusterStatus.Degraded);
    }

    [Fact]
    public void Handle_WithWarnings_IncludesWarnings()
    {
        var envId = Guid.NewGuid();
        var warning = new ClusterWarning(
            Code: "SlowConsumers",
            Severity: "Warning",
            Message: "server-1 has 15 slow consumers",
            ServerId: "server-1");

        var obs = BuildObservation(envId) with { Warnings = [warning] };
        _store.GetLatest(envId).Returns(obs);

        var result = _handler.Handle(new GetClusterOverviewQuery(envId));

        result!.Warnings.Count.ShouldBe(1);
        result.Warnings[0].Code.ShouldBe("SlowConsumers");
    }

    [Fact]
    public void Handle_UnavailableState_ReturnsUnavailableObservation()
    {
        var envId = Guid.NewGuid();
        var unavailableObs = BuildObservation(envId) with
        {
            Status = ClusterStatus.Unavailable,
            Freshness = ObservationFreshness.Unavailable,
            ServerCount = 0
        };
        _store.GetLatest(envId).Returns(unavailableObs);

        var result = _handler.Handle(new GetClusterOverviewQuery(envId));

        result!.Status.ShouldBe(ClusterStatus.Unavailable);
        result.Freshness.ShouldBe(ObservationFreshness.Unavailable);
    }
}

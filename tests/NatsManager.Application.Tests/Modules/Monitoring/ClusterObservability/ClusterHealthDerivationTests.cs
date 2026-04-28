using Shouldly;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

namespace NatsManager.Application.Tests.Modules.Monitoring.ClusterObservability;

public sealed class ClusterHealthDerivationTests
{
    private static ServerObservation BuildServer(
        string serverId = "server-1",
        ServerStatus status = ServerStatus.Healthy,
        ObservationFreshness freshness = ObservationFreshness.Live)
        => new(
            EnvironmentId: Guid.NewGuid(),
            ServerId: serverId,
            ServerName: serverId,
            ClusterName: "test-cluster",
            Version: "2.10.0",
            UptimeSeconds: 3600,
            Status: status,
            Freshness: freshness,
            Connections: 10,
            MaxConnections: 1000,
            SlowConsumers: 0,
            MemoryBytes: 1024 * 1024,
            StorageBytes: null,
            InMsgsPerSecond: 100.0,
            OutMsgsPerSecond: 50.0,
            InBytesPerSecond: 10240.0,
            OutBytesPerSecond: 5120.0,
            LastObservedAt: DateTimeOffset.UtcNow,
            MetricStates: []);

    [Fact]
    public void DeriveClusterStatus_AllHealthy_ReturnsHealthy()
    {
        var servers = new List<ServerObservation> { BuildServer("s1"), BuildServer("s2") };

        var result = ClusterHealthDerivation.DeriveClusterStatus(servers);

        result.ShouldBe(ClusterStatus.Healthy);
    }

    [Fact]
    public void DeriveClusterStatus_OneDegraded_ReturnsDegraded()
    {
        var servers = new List<ServerObservation>
        {
            BuildServer("s1"),
            BuildServer("s2", status: ServerStatus.Unavailable, freshness: ObservationFreshness.Unavailable)
        };

        var result = ClusterHealthDerivation.DeriveClusterStatus(servers);

        result.ShouldBe(ClusterStatus.Degraded);
    }

    [Fact]
    public void DeriveClusterStatus_AllUnavailable_ReturnsUnavailable()
    {
        var servers = new List<ServerObservation>
        {
            BuildServer("s1", status: ServerStatus.Unavailable, freshness: ObservationFreshness.Unavailable),
        };

        var result = ClusterHealthDerivation.DeriveClusterStatus(servers);

        result.ShouldBe(ClusterStatus.Unavailable);
    }

    [Fact]
    public void DeriveClusterStatus_EmptyList_ReturnsUnknown()
    {
        var result = ClusterHealthDerivation.DeriveClusterStatus([]);

        result.ShouldBe(ClusterStatus.Unknown);
    }

    [Fact]
    public void DeriveFreshness_AllLive_ReturnsLive()
    {
        var servers = new List<ServerObservation> { BuildServer("s1"), BuildServer("s2") };

        var result = ClusterHealthDerivation.DeriveFreshness(servers);

        result.ShouldBe(ObservationFreshness.Live);
    }

    [Fact]
    public void DeriveFreshness_MixedLiveAndStale_ReturnsPartial()
    {
        var servers = new List<ServerObservation>
        {
            BuildServer("s1"),
            BuildServer("s2", freshness: ObservationFreshness.Stale)
        };

        var result = ClusterHealthDerivation.DeriveFreshness(servers);

        result.ShouldBeOneOf(ObservationFreshness.Stale, ObservationFreshness.Partial);
    }

    [Fact]
    public void DeriveFreshness_AllUnavailable_ReturnsUnavailable()
    {
        var servers = new List<ServerObservation>
        {
            BuildServer("s1", freshness: ObservationFreshness.Unavailable)
        };

        var result = ClusterHealthDerivation.DeriveFreshness(servers);

        result.ShouldBe(ObservationFreshness.Unavailable);
    }

    [Fact]
    public void DeriveRate_WithValidBaseline_ReturnsRate()
    {
        var current = 1100L;
        var previous = 1000L;
        var intervalSeconds = 10.0;

        var rate = ClusterHealthDerivation.DeriveRate(current, previous, intervalSeconds);

        rate.ShouldBe(10.0); // (1100 - 1000) / 10
    }

    [Fact]
    public void DeriveRate_WhenCurrentLessThanPrevious_ReturnsNull()
    {
        // Counter wrap-around or reset
        var rate = ClusterHealthDerivation.DeriveRate(900L, 1000L, 10.0);

        rate.ShouldBeNull();
    }

    [Fact]
    public void DeriveRate_ZeroInterval_ReturnsNull()
    {
        var rate = ClusterHealthDerivation.DeriveRate(1100L, 1000L, 0);

        rate.ShouldBeNull();
    }

    [Fact]
    public void DeriveRate_NullInputs_ReturnsNull()
    {
        ClusterHealthDerivation.DeriveRate(null, 1000L, 10.0).ShouldBeNull();
        ClusterHealthDerivation.DeriveRate(1100L, null, 10.0).ShouldBeNull();
        ClusterHealthDerivation.DeriveRate(null, null, 10.0).ShouldBeNull();
    }

    [Fact]
    public void DeriveServerFreshness_Recent_ReturnsLive()
    {
        var now = DateTimeOffset.UtcNow;
        var lastObservedAt = now.AddSeconds(-5);

        var freshness = ClusterHealthDerivation.DeriveServerFreshness(lastObservedAt, now, staleThresholdSeconds: 30);

        freshness.ShouldBe(ObservationFreshness.Live);
    }

    [Fact]
    public void DeriveServerFreshness_TooOld_ReturnsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var lastObservedAt = now.AddSeconds(-60);

        var freshness = ClusterHealthDerivation.DeriveServerFreshness(lastObservedAt, now, staleThresholdSeconds: 30);

        freshness.ShouldBe(ObservationFreshness.Stale);
    }
}

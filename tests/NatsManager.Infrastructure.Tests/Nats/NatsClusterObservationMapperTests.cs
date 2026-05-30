using System.Text.Json;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Infrastructure.Nats.ClusterObservability;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class NatsClusterObservationMapperTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.UnixEpoch;

    [Fact]
    public void ExtractClusterName_WhenNull_ReturnsNull() =>
        NatsClusterObservationMapper.ExtractClusterName(null).ShouldBeNull();

    [Fact]
    public void ExtractClusterName_WhenString_ReturnsValue()
    {
        var element = JsonDocument.Parse("\"east\"").RootElement;
        NatsClusterObservationMapper.ExtractClusterName(element).ShouldBe("east");
    }

    [Fact]
    public void ExtractClusterName_WhenObjectWithName_ReturnsName()
    {
        var element = JsonDocument.Parse("""{"name":"west"}""").RootElement;
        NatsClusterObservationMapper.ExtractClusterName(element).ShouldBe("west");
    }

    [Fact]
    public void ExtractClusterName_WhenObjectWithoutName_ReturnsNull()
    {
        var element = JsonDocument.Parse("""{"id":"x"}""").RootElement;
        NatsClusterObservationMapper.ExtractClusterName(element).ShouldBeNull();
    }

    [Fact]
    public void BuildServerObservation_FlagsWarningWhenSlowConsumersPresent()
    {
        var varz = CreateVarz(slowConsumers: 3);

        var server = NatsClusterObservationMapper.BuildServerObservation(EnvironmentId, varz, ObservedAt);

        server.Status.ShouldBe(ServerStatus.Warning);
        server.SlowConsumers.ShouldBe(3);
        server.ServerId.ShouldBe("srv-1");
    }

    [Fact]
    public void BuildServerObservation_DefaultsUnknownServerIdAndHealthyStatus()
    {
        var varz = CreateVarz(serverId: null, slowConsumers: 0);

        var server = NatsClusterObservationMapper.BuildServerObservation(EnvironmentId, varz, ObservedAt);

        server.ServerId.ShouldBe("unknown");
        server.Status.ShouldBe(ServerStatus.Healthy);
        server.MaxConnections.ShouldBeNull();
    }

    [Fact]
    public void BuildTopologyRelationships_MapsRoutesGatewaysAndLeafs()
    {
        var routez = new ClusterRoutezResponse([new SafeRouteInfo("r1", "remote-1", true, false)]);
        var gatewayz = new ClusterGatewayzResponse("gw", [new SafeGatewayInfo("g1", true, "CONNECTED")]);
        var leafz = new ClusterLeafzResponse([new SafeLeafInfo("l1", null, true, 0, 0)]);

        var relationships = NatsClusterObservationMapper.BuildTopologyRelationships(
            EnvironmentId, routez, gatewayz, leafz, ObservedAt);

        relationships.Count.ShouldBe(3);
        relationships.ShouldContain(r => r.Type == TopologyRelationshipType.Route && r.TargetNodeId == "r1");
        relationships.ShouldContain(r => r.Type == TopologyRelationshipType.Gateway && r.Status == RelationshipStatus.Healthy);
        relationships.ShouldContain(r => r.Type == TopologyRelationshipType.LeafNode && r.Direction == RelationshipDirection.Inbound);
    }

    [Fact]
    public void BuildTopologyRelationships_MarksDisconnectedGatewayAsWarning()
    {
        var gatewayz = new ClusterGatewayzResponse("gw", [new SafeGatewayInfo("g1", true, "DISCONNECTED")]);

        var relationships = NatsClusterObservationMapper.BuildTopologyRelationships(
            EnvironmentId, null, gatewayz, null, ObservedAt);

        relationships.ShouldHaveSingleItem().Status.ShouldBe(RelationshipStatus.Warning);
    }

    [Fact]
    public void DeriveWarnings_RaisesSlowConsumerWarningAtThreshold()
    {
        var opts = new MonitoringOptions();
        var server = CreateServer(slowConsumers: opts.SlowConsumerWarningThreshold);

        var warnings = NatsClusterObservationMapper.DeriveWarnings([server], opts);

        warnings.ShouldContain(w => w.Code == "SlowConsumers");
    }

    [Fact]
    public void DeriveWarnings_RaisesConnectionPressureWarning()
    {
        var opts = new MonitoringOptions();
        var server = CreateServer(connections: 100, maxConnections: 100);

        var warnings = NatsClusterObservationMapper.DeriveWarnings([server], opts);

        warnings.ShouldContain(w => w.Code == "ConnectionPressure");
    }

    [Fact]
    public void DeriveWarnings_RaisesStaleServerWarning()
    {
        var server = CreateServer(freshness: ObservationFreshness.Stale);

        var warnings = NatsClusterObservationMapper.DeriveWarnings([server], new MonitoringOptions());

        warnings.ShouldContain(w => w.Code == "StaleServer");
    }

    [Fact]
    public void DeriveWarnings_ReturnsEmptyForHealthyServer()
    {
        var warnings = NatsClusterObservationMapper.DeriveWarnings([CreateServer()], new MonitoringOptions());

        warnings.ShouldBeEmpty();
    }

    [Fact]
    public void MaskUrl_MasksUserInfo()
    {
        NatsClusterObservationMapper.MaskUrl("nats://user:pass@host:4222/path")
            .ShouldBe("nats://***@host:4222/path");
    }

    [Fact]
    public void MaskUrl_LeavesUrlWithoutUserInfoUnchanged()
    {
        NatsClusterObservationMapper.MaskUrl("nats://host:4222").ShouldBe("nats://host:4222");
    }

    [Fact]
    public void MaskUrl_ReturnsNullForNull() =>
        NatsClusterObservationMapper.MaskUrl(null).ShouldBeNull();

    private static ClusterVarzResponse CreateVarz(string? serverId = "srv-1", int slowConsumers = 0) =>
        new(
            ServerId: serverId,
            ServerName: "n1",
            ClusterName: null,
            Version: "2.12.1",
            Uptime: "1m",
            UptimeSeconds: 60,
            Connections: 2,
            MaxConnections: 0,
            SlowConsumers: slowConsumers,
            InMsgs: 0,
            OutMsgs: 0,
            InBytes: 0,
            OutBytes: 0,
            Mem: 0);

    private static ServerObservation CreateServer(
        int slowConsumers = 0,
        int? connections = null,
        int? maxConnections = null,
        ObservationFreshness freshness = ObservationFreshness.Live) =>
        new(
            EnvironmentId: EnvironmentId,
            ServerId: "srv-1",
            ServerName: "n1",
            ClusterName: null,
            Version: "2.12.1",
            UptimeSeconds: 60,
            Status: ServerStatus.Healthy,
            Freshness: freshness,
            Connections: connections,
            MaxConnections: maxConnections,
            SlowConsumers: slowConsumers,
            MemoryBytes: null,
            StorageBytes: null,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            InBytesPerSecond: null,
            OutBytesPerSecond: null,
            LastObservedAt: ObservedAt,
            MetricStates: [MetricState.Live]);
}

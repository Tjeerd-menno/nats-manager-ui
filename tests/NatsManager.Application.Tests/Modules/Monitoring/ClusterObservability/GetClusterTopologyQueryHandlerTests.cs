using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

namespace NatsManager.Application.Tests.Modules.Monitoring.ClusterObservability;

public sealed class GetClusterTopologyQueryHandlerTests
{
    private readonly IClusterObservationStore _store = Substitute.For<IClusterObservationStore>();
    private readonly GetClusterTopologyQueryHandler _handler;

    public GetClusterTopologyQueryHandlerTests()
    {
        _handler = new GetClusterTopologyQueryHandler(_store);
    }

    private async Task<TestOutputPort<ClusterTopologyGraphResult>> ExecuteAsync(GetClusterTopologyQuery query)
    {
        var outputPort = new TestOutputPort<ClusterTopologyGraphResult>();
        await _handler.ExecuteAsync(query, outputPort);
        return outputPort;
    }

    private static ClusterObservation BuildObservation(
        Guid environmentId,
        IReadOnlyList<TopologyRelationship>? topology = null,
        IReadOnlyList<ServerObservation>? servers = null)
        => new(
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
            Servers: servers ?? [],
            Topology: topology ?? []);

    private static TopologyRelationship BuildRelationship(
        Guid environmentId,
        string sourceNodeId = "server-1",
        string targetNodeId = "server-2",
        TopologyRelationshipType type = TopologyRelationshipType.Route,
        RelationshipStatus status = RelationshipStatus.Healthy,
        ObservationFreshness freshness = ObservationFreshness.Live)
        => new(
            EnvironmentId: environmentId,
            RelationshipId: $"{sourceNodeId}-{targetNodeId}",
            SourceNodeId: sourceNodeId,
            TargetNodeId: targetNodeId,
            Type: type,
            Direction: RelationshipDirection.Bidirectional,
            Status: status,
            Freshness: freshness,
            ObservedAt: DateTimeOffset.UtcNow,
            SourceEndpoint: MonitoringEndpoint.Routez,
            SafeLabel: $"{sourceNodeId} → {targetNodeId}");

    [Fact]
    public async Task ExecuteAsync_WhenStoreEmpty_EmitsUnavailable()
    {
        var envId = Guid.NewGuid();
        _store.GetLatest(envId).Returns((ClusterObservation?)null);

        var output = await ExecuteAsync(new GetClusterTopologyQuery(envId));

        output.IsSuccess.ShouldBeFalse();
        output.IsUnavailable.ShouldBeTrue();
        output.Value.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoreHasData_ReturnsCompleteTopology()
    {
        var envId = Guid.NewGuid();
        var rel = BuildRelationship(envId, "server-1", "server-2");
        var obs = BuildObservation(envId, topology: [rel]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId))).Value;

        result.ShouldNotBeNull();
        result!.EnvironmentId.ShouldBe(envId);
        result.Edges.Count.ShouldBe(1);
        result.Nodes.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithTypeFilter_ReturnsOnlyMatchingRelationships()
    {
        var envId = Guid.NewGuid();
        var route = BuildRelationship(envId, "s1", "s2", TopologyRelationshipType.Route);
        var gateway = BuildRelationship(envId, "s3", "s4", TopologyRelationshipType.Gateway);
        var obs = BuildObservation(envId, topology: [route, gateway]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId, Types: [TopologyRelationshipType.Route]))).Value;

        result.ShouldNotBeNull();
        result!.Edges.Count.ShouldBe(1);
        result.Edges[0].Type.ShouldBe(TopologyRelationshipType.Route);
    }

    [Fact]
    public async Task ExecuteAsync_WithStatusFilter_ReturnsOnlyMatchingRelationships()
    {
        var envId = Guid.NewGuid();
        var healthy = BuildRelationship(envId, "s1", "s2", status: RelationshipStatus.Healthy);
        var warning = BuildRelationship(envId, "s3", "s4", status: RelationshipStatus.Warning);
        var obs = BuildObservation(envId, topology: [healthy, warning]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId, Status: RelationshipStatus.Warning))).Value;

        result.ShouldNotBeNull();
        result!.Edges.Count.ShouldBe(1);
        result.Edges[0].Status.ShouldBe(RelationshipStatus.Warning);
    }

    [Fact]
    public async Task ExecuteAsync_WithIncludeStaleSetToFalse_ExcludesStaleRelationships()
    {
        var envId = Guid.NewGuid();
        var live = BuildRelationship(envId, "s1", "s2", freshness: ObservationFreshness.Live);
        var stale = BuildRelationship(envId, "s3", "s4", freshness: ObservationFreshness.Stale);
        var obs = BuildObservation(envId, topology: [live, stale]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId, IncludeStale: false))).Value;

        result.ShouldNotBeNull();
        result!.Edges.Count.ShouldBe(1);
        result.Edges[0].Freshness.ShouldBe(ObservationFreshness.Live);
    }

    [Fact]
    public async Task ExecuteAsync_WithMaxNodesLimit_TruncatesNodes()
    {
        var envId = Guid.NewGuid();
        // Four distinct node IDs: s1, s2, s3, s4
        var rels = new List<TopologyRelationship>
        {
            BuildRelationship(envId, "s1", "s2"),
            BuildRelationship(envId, "s3", "s4"),
        };
        var obs = BuildObservation(envId, topology: rels);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId, MaxNodes: 2))).Value;

        result.ShouldNotBeNull();
        result!.Nodes.Count.ShouldBe(2);
        result.OmittedCounts.FilteredNodes.ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_WithServerObservations_BuildsServerNodes()
    {
        var envId = Guid.NewGuid();
        var server = new ServerObservation(
            EnvironmentId: envId,
            ServerId: "server-1",
            ServerName: "nats-server-1",
            ClusterName: "my-cluster",
            Version: "2.10.0",
            UptimeSeconds: 3600,
            Status: ServerStatus.Healthy,
            Freshness: ObservationFreshness.Live,
            Connections: 10,
            MaxConnections: 1000,
            SlowConsumers: 0,
            MemoryBytes: 1024,
            StorageBytes: null,
            InMsgsPerSecond: 100.0,
            OutMsgsPerSecond: 50.0,
            InBytesPerSecond: 10240.0,
            OutBytesPerSecond: 5120.0,
            LastObservedAt: DateTimeOffset.UtcNow,
            MetricStates: []);

        var rel = BuildRelationship(envId, "server-1", "server-2");
        var obs = BuildObservation(envId, topology: [rel], servers: [server]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId))).Value;

        result.ShouldNotBeNull();
        var serverNode = result!.Nodes.First(n => n.Id == "server-1");
        serverNode.Type.ShouldBe("server");
        serverNode.Label.ShouldBe("nats-server-1");
        serverNode.ServerId.ShouldBe("server-1");
        serverNode.Metadata.ShouldContainKey("version");
        serverNode.Metadata.ShouldContainKey("clusterName");
    }

    [Theory]
    [InlineData("gateway-eu-west", "gateway")]
    [InlineData("leaf-branch-1", "leafnode")]
    [InlineData("route-peer-2", "routePeer")]
    [InlineData("unknown-node-x", "external")]
    public async Task ExecuteAsync_DeterminesNodeTypeByIdPrefix(string nodeId, string expectedType)
    {
        var envId = Guid.NewGuid();
        var rel = BuildRelationship(envId, nodeId, "server-2");
        var obs = BuildObservation(envId, topology: [rel]);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId))).Value;

        result.ShouldNotBeNull();
        var node = result!.Nodes.First(n => n.Id == nodeId);
        node.Type.ShouldBe(expectedType);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTopology_ReturnsEmptyGraph()
    {
        var envId = Guid.NewGuid();
        var obs = BuildObservation(envId, topology: []);
        _store.GetLatest(envId).Returns(obs);

        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId))).Value;

        result.ShouldNotBeNull();
        result!.Nodes.ShouldBeEmpty();
        result.Edges.ShouldBeEmpty();
        result.OmittedCounts.FilteredNodes.ShouldBe(0);
        result.OmittedCounts.FilteredEdges.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_MaxNodesClampedToAtLeastOne()
    {
        var envId = Guid.NewGuid();
        var rel = BuildRelationship(envId, "s1", "s2");
        var obs = BuildObservation(envId, topology: [rel]);
        _store.GetLatest(envId).Returns(obs);

        // MaxNodes=0 should be clamped to 1 (not throw or return 0 nodes from a non-empty graph)
        var result = (await ExecuteAsync(new GetClusterTopologyQuery(envId, MaxNodes: 0))).Value;

        result.ShouldNotBeNull();
        result!.Nodes.Count.ShouldBeGreaterThan(0);
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Infrastructure.Relationships;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class RelationshipProjectionServiceTests
{
    [Fact]
    public async Task ProjectAsync_WhenEdgeTargetCannotBeResolved_ShouldOmitDanglingEdge()
    {
        var environmentId = Guid.NewGuid();
        var focal = new FocalResource(environmentId, ResourceType.KvBucket, "qa-bucket", "qa-bucket", "/kv/buckets/qa-bucket");
        var focalNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.KvBucket, "qa-bucket");
        var missingNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.KvKey, "qa-bucket/hello");
        var edge = new RelationshipEdge(
            RelationshipEdge.BuildEdgeId(focalNodeId, missingNodeId, RelationshipType.Contains, RelationshipSourceModule.KeyValue),
            environmentId,
            focalNodeId,
            missingNodeId,
            RelationshipType.Contains,
            RelationshipDirection.Outbound,
            ObservationKind.Observed,
            RelationshipConfidence.High,
            RelationshipFreshness.Live,
            ResourceHealthStatus.Healthy,
            [
                new RelationshipEvidence(
                    RelationshipSourceModule.KeyValue,
                    "KvKeyInBucket",
                    DateTimeOffset.UtcNow,
                    RelationshipFreshness.Live,
                    "Key hello exists in bucket qa-bucket",
                    new Dictionary<string, string>())
            ]);
        var service = new RelationshipProjectionService(
            [new FakeRelationshipSource([edge], [])],
            NullLogger<RelationshipProjectionService>.Instance);

        var map = await service.ProjectAsync(focal, MapFilter.Default, CancellationToken.None);

        map.Nodes.Select(node => node.NodeId).ShouldContain(focalNodeId);
        map.Edges.ShouldBeEmpty();
        map.OmittedCounts.FilteredEdges.ShouldBe(1);
    }

    [Fact]
    public async Task ProjectAsync_WhenFocalNodeHasIncidentStatus_ShouldMarkDirectNeighborsAndKeepFocalHighlighted()
    {
        var environmentId = Guid.NewGuid();
        var focal = new FocalResource(
            environmentId,
            ResourceType.Consumer,
            "orders/billing",
            "billing",
            "/jetstream/streams/orders/consumers/billing");
        var focalNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Consumer, "orders/billing");
        var streamNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Stream, "orders");
        var subjectNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Subject, "orders.created");
        var service = new RelationshipProjectionService(
            [new FakeRelationshipSource(
                [
                    CreateEdge(environmentId, focalNodeId, streamNodeId, RelationshipType.BackedByStream),
                    CreateEdge(environmentId, focalNodeId, subjectNodeId, RelationshipType.UsesSubject),
                ],
                [
                    CreateNode(environmentId, focalNodeId, ResourceType.Consumer, "orders/billing", "billing", ResourceHealthStatus.Warning),
                    CreateNode(environmentId, streamNodeId, ResourceType.Stream, "orders", "orders"),
                    CreateNode(environmentId, subjectNodeId, ResourceType.Subject, "orders.created", "orders.created"),
                ])],
            NullLogger<RelationshipProjectionService>.Instance);

        var map = await service.ProjectAsync(focal, MapFilter.Default, CancellationToken.None);

        map.Nodes.Single(node => node.NodeId == focalNodeId).IsFocal.ShouldBeTrue();
        map.Nodes.Single(node => node.NodeId == streamNodeId).Status.ShouldBe(ResourceHealthStatus.Warning);
        map.Nodes.Single(node => node.NodeId == subjectNodeId).Status.ShouldBe(ResourceHealthStatus.Warning);
    }

    [Fact]
    public async Task ProjectAsync_WhenDirectEdgeHasIncidentStatus_ShouldPromoteNeighborStatus()
    {
        var environmentId = Guid.NewGuid();
        var focal = new FocalResource(
            environmentId,
            ResourceType.Stream,
            "orders",
            "orders",
            "/jetstream/streams/orders");
        var focalNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Stream, "orders");
        var subjectNodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Subject, "orders.created");
        var service = new RelationshipProjectionService(
            [new FakeRelationshipSource(
                [
                    CreateEdge(
                        environmentId,
                        focalNodeId,
                        subjectNodeId,
                        RelationshipType.UsesSubject,
                        status: ResourceHealthStatus.Stale),
                ],
                [
                    CreateNode(environmentId, focalNodeId, ResourceType.Stream, "orders", "orders"),
                    CreateNode(environmentId, subjectNodeId, ResourceType.Subject, "orders.created", "orders.created"),
                ])],
            NullLogger<RelationshipProjectionService>.Instance);

        var map = await service.ProjectAsync(focal, MapFilter.Default, CancellationToken.None);

        map.Nodes.Single(node => node.NodeId == subjectNodeId).Status.ShouldBe(ResourceHealthStatus.Stale);
    }

    private static RelationshipEdge CreateEdge(
        Guid environmentId,
        string sourceNodeId,
        string targetNodeId,
        RelationshipType relationshipType,
        ResourceHealthStatus status = ResourceHealthStatus.Healthy) =>
        new(
            RelationshipEdge.BuildEdgeId(sourceNodeId, targetNodeId, relationshipType, RelationshipSourceModule.KeyValue),
            environmentId,
            sourceNodeId,
            targetNodeId,
            relationshipType,
            RelationshipDirection.Outbound,
            ObservationKind.Observed,
            RelationshipConfidence.High,
            RelationshipFreshness.Live,
            status,
            [
                new RelationshipEvidence(
                    RelationshipSourceModule.KeyValue,
                    relationshipType.ToString(),
                    DateTimeOffset.UtcNow,
                    RelationshipFreshness.Live,
                    "Relationship observed",
                    new Dictionary<string, string>())
            ]);

    private static ResourceNode CreateNode(
        Guid environmentId,
        string nodeId,
        ResourceType resourceType,
        string resourceId,
        string displayName,
        ResourceHealthStatus status = ResourceHealthStatus.Healthy) =>
        new(
            nodeId,
            environmentId,
            resourceType,
            resourceId,
            displayName,
            status,
            RelationshipFreshness.Live,
            false,
            null,
            new Dictionary<string, string>());

    private sealed class FakeRelationshipSource(
        IReadOnlyList<RelationshipEdge> edges,
        IReadOnlyList<ResourceNode> nodes) : IRelationshipSource
    {
        public RelationshipSourceModule SourceModule => RelationshipSourceModule.KeyValue;

        public Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
            FocalResource focal,
            MapFilter filters,
            CancellationToken ct) =>
            Task.FromResult(edges);

        public Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
            IEnumerable<string> nodeIds,
            Guid environmentId,
            CancellationToken ct) =>
            Task.FromResult(nodes);
    }
}

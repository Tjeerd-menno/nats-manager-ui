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

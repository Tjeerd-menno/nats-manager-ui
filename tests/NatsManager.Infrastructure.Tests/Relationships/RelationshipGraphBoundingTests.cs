using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Infrastructure.Relationships;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class RelationshipGraphBoundingTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    [Fact]
    public void BuildNodeIds_AlwaysIncludesFocalAndEdgeEndpoints()
    {
        var focal = new FocalResource(EnvironmentId, ResourceType.Stream, "orders", "orders", null);
        var focalNodeId = ResourceNode.BuildNodeId(EnvironmentId, ResourceType.Stream, "orders");
        var edge = CreateEdge(focalNodeId, "node:Subject:orders.created");

        var nodeIds = RelationshipGraphBounding.BuildNodeIds(focal, [edge]);

        nodeIds.ShouldContain(focalNodeId);
        nodeIds.ShouldContain("node:Subject:orders.created");
        nodeIds.Count.ShouldBe(2);
    }

    [Fact]
    public void SelectIncludedNodeIds_PrioritizesFocalWhenTruncating()
    {
        var focalNodeId = "focal";
        var nodeIds = new HashSet<string> { "a", "b", focalNodeId, "c" };

        var included = RelationshipGraphBounding.SelectIncludedNodeIds(nodeIds, focalNodeId, maxNodes: 1);

        included.ShouldHaveSingleItem();
        included.ShouldContain(focalNodeId);
    }

    [Fact]
    public void FilterEdgesByIncludedNodes_DropsEdgesWithExcludedEndpoint()
    {
        var included = new HashSet<string> { "a", "b" };
        var keep = CreateEdge("a", "b");
        var drop = CreateEdge("a", "z");

        var (edges, filtered) = RelationshipGraphBounding.FilterEdgesByIncludedNodes([keep, drop], included);

        edges.ShouldHaveSingleItem();
        edges[0].ShouldBe(keep);
        filtered.ShouldBe(1);
    }

    [Fact]
    public void RemoveDanglingEdges_DropsEdgesWhoseEndpointsAreNotResolved()
    {
        var keep = CreateEdge("a", "b");
        var dangling = CreateEdge("a", "missing");
        var resolved = new Dictionary<string, ResourceNode>
        {
            ["a"] = CreateNode("a"),
            ["b"] = CreateNode("b"),
        };

        var (edges, filtered) = RelationshipGraphBounding.RemoveDanglingEdges([keep, dangling], resolved);

        edges.ShouldHaveSingleItem();
        edges[0].ShouldBe(keep);
        filtered.ShouldBe(1);
    }

    [Fact]
    public void EnsureFocalNode_AddsFocalWhenMissing()
    {
        var focal = new FocalResource(EnvironmentId, ResourceType.Stream, "orders", "orders", "/route");
        var focalNodeId = ResourceNode.BuildNodeId(EnvironmentId, ResourceType.Stream, "orders");
        var resolved = new Dictionary<string, ResourceNode>();

        RelationshipGraphBounding.EnsureFocalNode(resolved, focal, focalNodeId);

        resolved.ShouldContainKey(focalNodeId);
        resolved[focalNodeId].IsFocal.ShouldBeTrue();
        resolved[focalNodeId].Status.ShouldBe(ResourceHealthStatus.Unknown);
    }

    [Fact]
    public void EnsureFocalNode_DoesNotOverwriteExistingFocal()
    {
        var focal = new FocalResource(EnvironmentId, ResourceType.Stream, "orders", "orders", "/route");
        var focalNodeId = ResourceNode.BuildNodeId(EnvironmentId, ResourceType.Stream, "orders");
        var existing = CreateNode(focalNodeId, ResourceHealthStatus.Degraded);
        var resolved = new Dictionary<string, ResourceNode> { [focalNodeId] = existing };

        RelationshipGraphBounding.EnsureFocalNode(resolved, focal, focalNodeId);

        resolved[focalNodeId].ShouldBe(existing);
    }

    [Theory]
    [InlineData("source", "target", "source", "target")]
    [InlineData("source", "target", "target", "source")]
    public void GetNeighborNodeId_ReturnsOppositeEndpoint(
        string sourceNodeId, string targetNodeId, string focalNodeId, string expected)
    {
        var edge = CreateEdge(sourceNodeId, targetNodeId);

        RelationshipGraphBounding.GetNeighborNodeId(edge, focalNodeId).ShouldBe(expected);
    }

    private static RelationshipEdge CreateEdge(string sourceNodeId, string targetNodeId) =>
        new(
            RelationshipEdge.BuildEdgeId(sourceNodeId, targetNodeId, RelationshipType.UsesSubject, RelationshipSourceModule.KeyValue),
            EnvironmentId,
            sourceNodeId,
            targetNodeId,
            RelationshipType.UsesSubject,
            RelationshipDirection.Outbound,
            ObservationKind.Observed,
            RelationshipConfidence.High,
            RelationshipFreshness.Live,
            ResourceHealthStatus.Healthy,
            []);

    private static ResourceNode CreateNode(string nodeId, ResourceHealthStatus status = ResourceHealthStatus.Healthy) =>
        new(
            nodeId,
            EnvironmentId,
            ResourceType.Subject,
            nodeId,
            nodeId,
            status,
            RelationshipFreshness.Live,
            false,
            null,
            new Dictionary<string, string>());
}

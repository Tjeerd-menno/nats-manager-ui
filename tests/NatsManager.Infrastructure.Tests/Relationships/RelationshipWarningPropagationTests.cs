using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Infrastructure.Relationships;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class RelationshipWarningPropagationTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    [Fact]
    public void PropagateWarningStates_WhenFocalHasIncidentStatus_PromotesHealthyNeighbor()
    {
        const string focalNodeId = "focal";
        const string neighborNodeId = "neighbor";
        var nodes = new Dictionary<string, ResourceNode>
        {
            [focalNodeId] = CreateNode(focalNodeId, ResourceHealthStatus.Warning),
            [neighborNodeId] = CreateNode(neighborNodeId, ResourceHealthStatus.Healthy),
        };
        var edges = new List<RelationshipEdge> { CreateEdge(focalNodeId, neighborNodeId) };

        RelationshipWarningPropagation.PropagateWarningStates(nodes, edges, focalNodeId);

        nodes[focalNodeId].IsFocal.ShouldBeTrue();
        nodes[neighborNodeId].Status.ShouldBe(ResourceHealthStatus.Warning);
    }

    [Fact]
    public void PropagateWarningStates_WhenFocalHealthy_LeavesNeighborUnchanged()
    {
        const string focalNodeId = "focal";
        const string neighborNodeId = "neighbor";
        var nodes = new Dictionary<string, ResourceNode>
        {
            [focalNodeId] = CreateNode(focalNodeId, ResourceHealthStatus.Healthy),
            [neighborNodeId] = CreateNode(neighborNodeId, ResourceHealthStatus.Healthy),
        };
        var edges = new List<RelationshipEdge> { CreateEdge(focalNodeId, neighborNodeId) };

        RelationshipWarningPropagation.PropagateWarningStates(nodes, edges, focalNodeId);

        nodes[neighborNodeId].Status.ShouldBe(ResourceHealthStatus.Healthy);
    }

    [Fact]
    public void PropagateWarningStates_PromotesNeighborToMostSevereStatusFromIncidentEdge()
    {
        const string focalNodeId = "focal";
        const string neighborNodeId = "neighbor";
        var nodes = new Dictionary<string, ResourceNode>
        {
            [focalNodeId] = CreateNode(focalNodeId, ResourceHealthStatus.Healthy),
            [neighborNodeId] = CreateNode(neighborNodeId, ResourceHealthStatus.Healthy),
        };
        var edges = new List<RelationshipEdge>
        {
            CreateEdge(focalNodeId, neighborNodeId, ResourceHealthStatus.Degraded),
        };

        RelationshipWarningPropagation.PropagateWarningStates(nodes, edges, focalNodeId);

        nodes[neighborNodeId].Status.ShouldBe(ResourceHealthStatus.Degraded);
    }

    [Fact]
    public void ApplyHealthStateFilterAfterPropagation_RemovesNodesAndEdgesOutsideHealthStates()
    {
        const string focalNodeId = "focal";
        const string neighborNodeId = "neighbor";
        var nodes = new Dictionary<string, ResourceNode>
        {
            [focalNodeId] = CreateNode(focalNodeId, ResourceHealthStatus.Healthy),
            [neighborNodeId] = CreateNode(neighborNodeId, ResourceHealthStatus.Warning),
        };
        var edges = new List<RelationshipEdge> { CreateEdge(focalNodeId, neighborNodeId) };
        var filters = MapFilter.Default with { HealthStates = [ResourceHealthStatus.Healthy] };

        var (remainingEdges, filteredNodes, filteredEdges) =
            RelationshipWarningPropagation.ApplyHealthStateFilterAfterPropagation(nodes, edges, filters);

        nodes.ShouldContainKey(focalNodeId);
        nodes.ShouldNotContainKey(neighborNodeId);
        remainingEdges.ShouldBeEmpty();
        filteredNodes.ShouldBe(1);
        filteredEdges.ShouldBe(1);
    }

    [Fact]
    public void ApplyHealthStateFilterAfterPropagation_WhenNoHealthStateFilter_KeepsEverything()
    {
        var nodes = new Dictionary<string, ResourceNode>
        {
            ["a"] = CreateNode("a", ResourceHealthStatus.Warning),
        };
        var edges = new List<RelationshipEdge> { CreateEdge("a", "a") };

        var (remainingEdges, filteredNodes, filteredEdges) =
            RelationshipWarningPropagation.ApplyHealthStateFilterAfterPropagation(nodes, edges, MapFilter.Default);

        remainingEdges.Count.ShouldBe(1);
        filteredNodes.ShouldBe(0);
        filteredEdges.ShouldBe(0);
    }

    private static RelationshipEdge CreateEdge(
        string sourceNodeId,
        string targetNodeId,
        ResourceHealthStatus status = ResourceHealthStatus.Healthy) =>
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
            status,
            []);

    private static ResourceNode CreateNode(string nodeId, ResourceHealthStatus status) =>
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

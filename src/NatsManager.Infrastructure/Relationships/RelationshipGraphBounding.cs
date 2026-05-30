using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Pure, stateless graph-bounding helpers used by <see cref="RelationshipProjectionService"/>:
/// node-set construction, MaxNodes/MaxEdges bounding, and dangling-edge removal. Extracting these
/// from the orchestrator keeps the projection pipeline readable and makes the bounding rules
/// independently unit-testable.
/// </summary>
internal static class RelationshipGraphBounding
{
    public static HashSet<string> BuildNodeIds(FocalResource focal, IEnumerable<RelationshipEdge> edges)
    {
        var nodeIds = new HashSet<string> { ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId) };
        foreach (var edge in edges)
        {
            nodeIds.Add(edge.SourceNodeId);
            nodeIds.Add(edge.TargetNodeId);
        }

        return nodeIds;
    }

    public static HashSet<string> SelectIncludedNodeIds(HashSet<string> nodeIds, string focalNodeId, int maxNodes) =>
        nodeIds
            .OrderBy(nodeId => nodeId == focalNodeId ? 0 : 1)
            .Take(maxNodes)
            .ToHashSet();

    public static (List<RelationshipEdge> Edges, int FilteredEdges) FilterEdgesByIncludedNodes(
        IEnumerable<RelationshipEdge> edges,
        HashSet<string> includedNodeIds)
    {
        var includedEdges = new List<RelationshipEdge>();
        var filteredEdges = 0;

        foreach (var edge in edges)
        {
            if (includedNodeIds.Contains(edge.SourceNodeId) && includedNodeIds.Contains(edge.TargetNodeId))
                includedEdges.Add(edge);
            else
                filteredEdges++;
        }

        return (includedEdges, filteredEdges);
    }

    public static void EnsureFocalNode(
        Dictionary<string, ResourceNode> resolvedNodes,
        FocalResource focal,
        string focalNodeId)
    {
        if (resolvedNodes.ContainsKey(focalNodeId))
        {
            return;
        }

        resolvedNodes[focalNodeId] = new ResourceNode(
            NodeId: focalNodeId,
            EnvironmentId: focal.EnvironmentId,
            ResourceType: focal.ResourceType,
            ResourceId: focal.ResourceId,
            DisplayName: focal.DisplayName,
            Status: ResourceHealthStatus.Unknown,
            Freshness: RelationshipFreshness.Live,
            IsFocal: true,
            DetailRoute: focal.Route,
            Metadata: new Dictionary<string, string>());
    }

    public static (List<RelationshipEdge> Edges, int FilteredEdges) RemoveDanglingEdges(
        List<RelationshipEdge> includedEdges,
        Dictionary<string, ResourceNode> resolvedNodes)
    {
        var remainingEdges = includedEdges
            .Where(edge => resolvedNodes.ContainsKey(edge.SourceNodeId) && resolvedNodes.ContainsKey(edge.TargetNodeId))
            .ToList();

        return (remainingEdges, includedEdges.Count - remainingEdges.Count);
    }

    public static string GetNeighborNodeId(RelationshipEdge edge, string focalNodeId) =>
        edge.SourceNodeId == focalNodeId
            ? edge.TargetNodeId
            : edge.SourceNodeId;
}

using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Pure neighbor warning-state propagation for incident traversal (US2). Given a resolved node set
/// and the focal node, propagates incident severity to direct neighbors and applies the
/// post-propagation health-state filter. Stateless and independently unit-testable.
/// </summary>
internal static class RelationshipWarningPropagation
{
    public static void PropagateWarningStates(
        Dictionary<string, ResourceNode> nodes,
        IReadOnlyList<RelationshipEdge> edges,
        string focalNodeId)
    {
        if (!nodes.TryGetValue(focalNodeId, out var focalNode))
            return;

        nodes[focalNodeId] = focalNode with { IsFocal = true };
        var focalHasIncidentStatus = IsIncidentStatus(focalNode.Status);

        foreach (var edge in edges.Where(edge => edge.SourceNodeId == focalNodeId || edge.TargetNodeId == focalNodeId))
        {
            var neighborNodeId = RelationshipGraphBounding.GetNeighborNodeId(edge, focalNodeId);

            if (!nodes.TryGetValue(neighborNodeId, out var neighborNode))
                continue;

            var propagatedStatus = GetPropagatedStatus(edge, focalHasIncidentStatus);

            if (!IsIncidentStatus(propagatedStatus))
                continue;

            nodes[neighborNodeId] = neighborNode with
            {
                Status = GetMoreSevereStatus(neighborNode.Status, propagatedStatus)
            };
        }
    }

    public static (List<RelationshipEdge> Edges, int FilteredNodes, int FilteredEdges) ApplyHealthStateFilterAfterPropagation(
        Dictionary<string, ResourceNode> nodes,
        List<RelationshipEdge> edges,
        MapFilter filters)
    {
        if (filters.HealthStates is not { Count: > 0 })
            return ([.. edges], 0, 0);

        var filteredNodeIds = nodes.Values
            .Where(node => !filters.HealthStates.Contains(node.Status))
            .Select(node => node.NodeId)
            .ToHashSet();

        if (filteredNodeIds.Count == 0)
            return ([.. edges], 0, 0);

        foreach (var filteredNodeId in filteredNodeIds)
            nodes.Remove(filteredNodeId);

        var remainingEdges = edges
            .Where(edge => !filteredNodeIds.Contains(edge.SourceNodeId) && !filteredNodeIds.Contains(edge.TargetNodeId))
            .ToList();

        return (remainingEdges, filteredNodeIds.Count, edges.Count - remainingEdges.Count);
    }

    private static bool IsIncidentStatus(ResourceHealthStatus status) =>
        status is ResourceHealthStatus.Warning
            or ResourceHealthStatus.Degraded
            or ResourceHealthStatus.Stale
            or ResourceHealthStatus.Unavailable;

    private static ResourceHealthStatus GetPropagatedStatus(RelationshipEdge edge, bool focalHasIncidentStatus)
    {
        if (IsIncidentStatus(edge.Status))
            return edge.Status;

        return focalHasIncidentStatus
            ? ResourceHealthStatus.Warning
            : ResourceHealthStatus.Healthy;
    }

    private static ResourceHealthStatus GetMoreSevereStatus(
        ResourceHealthStatus currentStatus,
        ResourceHealthStatus propagatedStatus) =>
        GetSeverity(currentStatus) >= GetSeverity(propagatedStatus)
            ? currentStatus
            : propagatedStatus;

    private static int GetSeverity(ResourceHealthStatus status) =>
        status switch
        {
            ResourceHealthStatus.Warning => 1,
            ResourceHealthStatus.Stale => 2,
            ResourceHealthStatus.Degraded => 3,
            ResourceHealthStatus.Unavailable => 4,
            _ => 0
        };
}

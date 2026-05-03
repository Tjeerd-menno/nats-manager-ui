using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

/// <summary>Query to get the topology graph for an environment.</summary>
public sealed record GetClusterTopologyQuery(
    Guid EnvironmentId,
    IReadOnlyList<TopologyRelationshipType>? Types = null,
    RelationshipStatus? Status = null,
    bool IncludeStale = true,
    int MaxNodes = 250);

/// <summary>Handler for GetClusterTopologyQuery.</summary>
public sealed class GetClusterTopologyQueryHandler(
    IClusterObservationStore store)
{
    public ClusterTopologyGraphResult? Handle(GetClusterTopologyQuery query)
    {
        var observation = store.GetLatest(query.EnvironmentId);
        if (observation is null)
            return null;

        var maxNodes = Math.Min(Math.Max(query.MaxNodes, 1), 1000);

        var allRelationships = observation.Topology.ToList();

        // Apply type filter
        if (query.Types is { Count: > 0 })
            allRelationships = [.. allRelationships.Where(r => query.Types.Contains(r.Type))];

        // Apply status filter
        if (query.Status.HasValue)
            allRelationships = [.. allRelationships.Where(r => r.Status == query.Status.Value)];

        // Apply stale filter
        if (!query.IncludeStale)
            allRelationships = [.. allRelationships.Where(r => r.Freshness != ObservationFreshness.Stale)];

        // Build node set from relationships
        var nodeIds = allRelationships
            .SelectMany(r => (string[])[r.SourceNodeId, r.TargetNodeId])
            .Distinct()
            .ToList();

        var filteredNodes = Math.Max(0, nodeIds.Count - maxNodes);
        nodeIds = [.. nodeIds.Take(maxNodes)];

        var nodeSet = nodeIds.ToHashSet();
        var filteredEdges = 0;
        var includedRelationships = new List<TopologyRelationship>();
        foreach (var rel in allRelationships)
        {
            if (nodeSet.Contains(rel.SourceNodeId) && nodeSet.Contains(rel.TargetNodeId))
                includedRelationships.Add(rel);
            else
                filteredEdges++;
        }

        // Build nodes from server observations
        var serverNodeMap = observation.Servers.ToDictionary(s => s.ServerId);
        var nodes = nodeIds.Select(nodeId =>
        {
            serverNodeMap.TryGetValue(nodeId, out var server);
            return new ClusterTopologyNodeResult(
                Id: nodeId,
                Type: server is not null ? "server" : DetermineNodeType(nodeId),
                Label: server?.ServerName ?? nodeId,
                Status: server?.Status.ToString() ?? "Unknown",
                ServerId: server?.ServerId,
                Metadata: server is not null
                    ? new Dictionary<string, object?> { ["version"] = server.Version, ["clusterName"] = server.ClusterName }
                    : []);
        }).ToList();

        return new ClusterTopologyGraphResult(
            EnvironmentId: query.EnvironmentId,
            ObservedAt: observation.ObservedAt,
            Freshness: observation.Freshness,
            Nodes: nodes,
            Edges: includedRelationships,
            OmittedCounts: new TopologyOmittedCounts(filteredNodes, filteredEdges, 0));
    }

    private static string DetermineNodeType(string nodeId)
    {
        if (nodeId.StartsWith("gateway-", StringComparison.Ordinal)) return "gateway";
        if (nodeId.StartsWith("leaf-", StringComparison.Ordinal)) return "leafnode";
        if (nodeId.StartsWith("route-", StringComparison.Ordinal)) return "routePeer";
        return "external";
    }
}

public sealed record ClusterTopologyGraphResult(
    Guid EnvironmentId,
    DateTimeOffset ObservedAt,
    ObservationFreshness Freshness,
    IReadOnlyList<ClusterTopologyNodeResult> Nodes,
    IReadOnlyList<TopologyRelationship> Edges,
    TopologyOmittedCounts OmittedCounts);

public sealed record ClusterTopologyNodeResult(
    string Id,
    string Type,
    string Label,
    string Status,
    string? ServerId,
    Dictionary<string, object?> Metadata);

public sealed record TopologyOmittedCounts(int FilteredNodes, int FilteredEdges, int UnsafeRelationships);

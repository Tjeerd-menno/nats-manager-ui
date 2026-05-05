using Microsoft.Extensions.Logging;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Orchestrates all registered IRelationshipSource instances to build a bounded relationship map.
/// Performs BFS up to the configured depth, applies filters, enforces MaxNodes/MaxEdges,
/// removes unsafe cross-environment relationships, and populates OmittedCounts.
/// Never crosses environment boundaries.
/// </summary>
public sealed partial class RelationshipProjectionService(
    IEnumerable<IRelationshipSource> sources,
    ILogger<RelationshipProjectionService> logger)
{
    private readonly IReadOnlyList<IRelationshipSource> _sources = [.. sources];

    public async Task<RelationshipMap> ProjectAsync(
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct)
    {
        LogProjectionStarted(focal.EnvironmentId, focal.ResourceType, filters.Depth, filters.MaxNodes, filters.MaxEdges);

        var generatedAt = DateTimeOffset.UtcNow;
        var allEdges = new List<RelationshipEdge>();
        var unsafeCount = 0;

        // BFS: start with focal, expand per depth
        var visitedNodeIds = new HashSet<string> { ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId) };
        var currentFrontier = new List<FocalResource> { focal };

        for (var depth = 0; depth < filters.Depth && currentFrontier.Count > 0; depth++)
        {
            var nextFrontier = new List<FocalResource>();

            foreach (var frontierNode in currentFrontier)
            {
                var frontierNodeId = ResourceNode.BuildNodeId(frontierNode.EnvironmentId, frontierNode.ResourceType, frontierNode.ResourceId);
                foreach (var source in _sources)
                {
                    IReadOnlyList<RelationshipEdge> edges;
                    try
                    {
                        edges = await source.GetEdgesForAsync(frontierNode, filters, ct);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        LogSourceFailed(focal.EnvironmentId, source.SourceModule);
                        continue;
                    }

                    LogSourceSucceeded(focal.EnvironmentId, source.SourceModule, edges.Count);

                    foreach (var edge in edges)
                    {
                        // Enforce environment isolation: reject cross-environment edges
                        if (edge.EnvironmentId != focal.EnvironmentId)
                        {
                            unsafeCount++;
                            LogRelationshipOmitted(
                                focal.EnvironmentId,
                                source.SourceModule,
                                "CrossEnvironment",
                                edge.RelationshipType);
                            continue;
                        }

                        // Apply filters
                        if (!PassesFilters(edge, filters))
                            continue;

                        allEdges.Add(edge);

                        // Discover new nodes for next BFS depth
                        var neighborNodeId = edge.SourceNodeId == frontierNodeId
                            ? edge.TargetNodeId
                            : edge.SourceNodeId;

                        if (visitedNodeIds.Add(neighborNodeId))
                        {
                            // Add to next frontier if we have more depth to explore
                            if (depth + 1 < filters.Depth)
                            {
                                var parts = neighborNodeId.Split(':', 3);
                                if (parts.Length == 3 && Enum.TryParse<ResourceType>(parts[1], ignoreCase: true, out var rt))
                                    nextFrontier.Add(new FocalResource(focal.EnvironmentId, rt, parts[2], parts[2], null));
                            }
                        }
                    }
                }
            }

            currentFrontier = nextFrontier;
        }

        // Deduplicate edges
        var uniqueEdges = allEdges
            .GroupBy(e => e.EdgeId)
            .Select(g => g.First())
            .ToList();

        // Build node set from edges + focal
        var nodeIds = new HashSet<string> { ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId) };
        foreach (var edge in uniqueEdges)
        {
            nodeIds.Add(edge.SourceNodeId);
            nodeIds.Add(edge.TargetNodeId);
        }

        // Apply MaxNodes/MaxEdges bounds
        var filteredNodes = Math.Max(0, nodeIds.Count - filters.MaxNodes);
        var includedNodeIds = nodeIds
            .OrderBy(n => n == ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId) ? 0 : 1)
            .Take(filters.MaxNodes)
            .ToHashSet();

        var includedNodeSet = includedNodeIds;
        var filteredEdges = 0;
        var includedEdges = new List<RelationshipEdge>();
        foreach (var edge in uniqueEdges)
        {
            if (includedNodeSet.Contains(edge.SourceNodeId) && includedNodeSet.Contains(edge.TargetNodeId))
                includedEdges.Add(edge);
            else
                filteredEdges++;
        }

        var truncatedEdges = Math.Max(0, includedEdges.Count - filters.MaxEdges);
        includedEdges = [.. includedEdges.Take(filters.MaxEdges)];

        // Resolve nodes from sources
        var resolvedNodes = await ResolveNodesAsync(includedNodeIds, focal, filters, ct);
        var focalNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId);

        // Ensure focal node is always present
        if (!resolvedNodes.ContainsKey(focalNodeId))
        {
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

        var danglingEdges = includedEdges.Count(edge =>
            !resolvedNodes.ContainsKey(edge.SourceNodeId) || !resolvedNodes.ContainsKey(edge.TargetNodeId));
        if (danglingEdges > 0)
        {
            includedEdges = [.. includedEdges.Where(edge =>
                resolvedNodes.ContainsKey(edge.SourceNodeId) && resolvedNodes.ContainsKey(edge.TargetNodeId))];
            filteredEdges += danglingEdges;
        }

        // Propagate neighbor warning states (for US2 incident traversal)
        PropagateWarningStates(resolvedNodes, includedEdges, focalNodeId);

        var finalNodes = resolvedNodes.Values.ToList();

        var relationshipMap = new RelationshipMap(
            EnvironmentId: focal.EnvironmentId,
            FocalResource: focal,
            GeneratedAt: generatedAt,
            Depth: filters.Depth,
            Nodes: finalNodes,
            Edges: includedEdges,
            Filters: filters,
            OmittedCounts: new OmittedCounts(
                FilteredNodes: filteredNodes,
                FilteredEdges: filteredEdges + truncatedEdges,
                CollapsedNodes: 0,
                CollapsedEdges: 0,
                UnsafeRelationships: unsafeCount));

        LogProjectionCompleted(
            focal.EnvironmentId,
            finalNodes.Count,
            includedEdges.Count,
            relationshipMap.OmittedCounts.FilteredNodes,
            relationshipMap.OmittedCounts.FilteredEdges,
            relationshipMap.OmittedCounts.CollapsedNodes,
            relationshipMap.OmittedCounts.CollapsedEdges,
            relationshipMap.OmittedCounts.UnsafeRelationships);

        return relationshipMap;
    }

    private static bool PassesFilters(RelationshipEdge edge, MapFilter filters)
    {
        if (!filters.IncludeInferred && edge.ObservationKind == ObservationKind.Inferred)
            return false;

        if (!filters.IncludeStale && edge.Freshness == RelationshipFreshness.Stale)
            return false;

        if (filters.RelationshipTypes is { Count: > 0 } && !filters.RelationshipTypes.Contains(edge.RelationshipType))
            return false;

        if (filters.MinimumConfidence != RelationshipConfidence.Unknown)
        {
            var minLevel = (int)filters.MinimumConfidence;
            var edgeLevel = (int)edge.Confidence;
            // High=0, Medium=1, Low=2, Unknown=3 — we want confidence >= minimum
            if (edgeLevel > minLevel && edge.Confidence != RelationshipConfidence.Unknown)
                return false;
        }

        return true;
    }

    private async Task<Dictionary<string, ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds,
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct)
    {
        var result = new Dictionary<string, ResourceNode>();

        foreach (var source in _sources)
        {
            try
            {
                var nodes = await source.ResolveNodesAsync(nodeIds, focal.EnvironmentId, ct);
                foreach (var node in nodes)
                {
                    // Apply health state filter
                    if (filters.HealthStates is { Count: > 0 } && !filters.HealthStates.Contains(node.Status))
                        continue;

                    var focalNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId);
                    var isFocal = node.NodeId == focalNodeId;
                    result.TryAdd(node.NodeId, node with { IsFocal = isFocal });
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogNodeResolutionFailed(focal.EnvironmentId, source.SourceModule);
            }
        }

        return result;
    }

    private static void PropagateWarningStates(
        Dictionary<string, ResourceNode> nodes,
        IReadOnlyList<RelationshipEdge> edges,
        string focalNodeId)
    {
        // Neighbor warning states are intentionally preserved on each ResourceNode and rendered inline
        // by the frontend WarningOverlay. The focal node's owning-module status is not mutated here.
        _ = nodes;
        _ = edges;
        _ = focalNodeId;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Building relationship map for environment {EnvironmentId}, resource type {ResourceType}, depth {Depth}, max nodes {MaxNodes}, max edges {MaxEdges}.")]
    private partial void LogProjectionStarted(Guid environmentId, ResourceType resourceType, int depth, int maxNodes, int maxEdges);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Relationship source {Module} returned {EdgeCount} edge(s) for environment {EnvironmentId}.")]
    private partial void LogSourceSucceeded(Guid environmentId, RelationshipSourceModule module, int edgeCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Relationship source {Module} failed for environment {EnvironmentId}.")]
    private partial void LogSourceFailed(Guid environmentId, RelationshipSourceModule module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Omitted relationship for environment {EnvironmentId} from source {Module}; reason {Reason}, relationship type {RelationshipType}.")]
    private partial void LogRelationshipOmitted(Guid environmentId, RelationshipSourceModule module, string reason, RelationshipType relationshipType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Node resolution failed for source {Module} in environment {EnvironmentId}.")]
    private partial void LogNodeResolutionFailed(Guid environmentId, RelationshipSourceModule module);

    [LoggerMessage(Level = LogLevel.Information, Message = "Relationship map built for environment {EnvironmentId}: {NodeCount} node(s), {EdgeCount} edge(s), omitted filtered nodes {FilteredNodes}, filtered edges {FilteredEdges}, collapsed nodes {CollapsedNodes}, collapsed edges {CollapsedEdges}, unsafe relationships {UnsafeRelationships}.")]
    private partial void LogProjectionCompleted(Guid environmentId, int nodeCount, int edgeCount, int filteredNodes, int filteredEdges, int collapsedNodes, int collapsedEdges, int unsafeRelationships);
}

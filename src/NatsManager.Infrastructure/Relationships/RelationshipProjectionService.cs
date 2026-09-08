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
        var expansion = await TraverseEdgesBfsAsync(focal, filters, ct);

        // Deduplicate edges
        var uniqueEdges = expansion.Edges
            .GroupBy(e => e.EdgeId)
            .Select(g => g.First())
            .ToList();

        // Build node set from edges + focal
        var nodeIds = RelationshipGraphBounding.BuildNodeIds(focal, uniqueEdges);

        // Apply MaxNodes/MaxEdges bounds
        var filteredNodes = Math.Max(0, nodeIds.Count - filters.MaxNodes);
        var focalNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId);
        var includedNodeIds = RelationshipGraphBounding.SelectIncludedNodeIds(nodeIds, focalNodeId, filters.MaxNodes);

        var boundedEdges = RelationshipGraphBounding.FilterEdgesByIncludedNodes(uniqueEdges, includedNodeIds);
        var filteredEdges = boundedEdges.FilteredEdges;
        var includedEdges = boundedEdges.Edges;

        // Resolve nodes from sources before MaxEdges truncation so dangling edges can be filtered first
        var resolvedNodes = await ResolveNodesAsync(includedNodeIds, focal, filters, ct);

        // Ensure focal node is always present
        RelationshipGraphBounding.EnsureFocalNode(resolvedNodes, focal, focalNodeId);

        // Filter out dangling edges BEFORE applying MaxEdges so truncation operates on valid edges only
        var danglingResult = RelationshipGraphBounding.RemoveDanglingEdges(includedEdges, resolvedNodes);
        includedEdges = danglingResult.Edges;
        filteredEdges += danglingResult.FilteredEdges;

        var truncatedEdges = Math.Max(0, includedEdges.Count - filters.MaxEdges);
        includedEdges = [.. includedEdges.Take(filters.MaxEdges)];

        // Propagate neighbor warning states (for US2 incident traversal)
        RelationshipWarningPropagation.PropagateWarningStates(resolvedNodes, includedEdges, focalNodeId);
        var postPropagationFilterResult = RelationshipWarningPropagation.ApplyHealthStateFilterAfterPropagation(resolvedNodes, includedEdges, filters);
        includedEdges = postPropagationFilterResult.Edges;
        filteredNodes += postPropagationFilterResult.FilteredNodes;
        filteredEdges += postPropagationFilterResult.FilteredEdges;

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
                UnsafeRelationships: expansion.UnsafeCount));

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

    private async Task<(List<RelationshipEdge> Edges, int UnsafeCount)> TraverseEdgesBfsAsync(
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct)
    {
        var allEdges = new List<RelationshipEdge>();
        var unsafeCount = 0;
        var visitedNodeIds = new HashSet<string> { ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId) };
        var currentFrontier = new List<FocalResource> { focal };

        for (var depth = 0; depth < filters.Depth && currentFrontier.Count > 0; depth++)
        {
            var result = await ExpandFrontierAsync(focal, filters, currentFrontier, visitedNodeIds, depth, ct);
            allEdges.AddRange(result.Edges);
            unsafeCount += result.UnsafeCount;
            currentFrontier = result.NextFrontier;
        }

        return (allEdges, unsafeCount);
    }

    private async Task<(List<RelationshipEdge> Edges, List<FocalResource> NextFrontier, int UnsafeCount)> ExpandFrontierAsync(
        FocalResource focal,
        MapFilter filters,
        IReadOnlyList<FocalResource> currentFrontier,
        HashSet<string> visitedNodeIds,
        int depth,
        CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();
        var nextFrontier = new List<FocalResource>();
        var unsafeCount = 0;

        foreach (var frontierNode in currentFrontier)
        {
            var result = await ExpandNodeAsync(focal, filters, frontierNode, visitedNodeIds, depth, ct);
            edges.AddRange(result.Edges);
            nextFrontier.AddRange(result.NextFrontier);
            unsafeCount += result.UnsafeCount;
        }

        return (edges, nextFrontier, unsafeCount);
    }

    private async Task<(List<RelationshipEdge> Edges, List<FocalResource> NextFrontier, int UnsafeCount)> ExpandNodeAsync(
        FocalResource focal,
        MapFilter filters,
        FocalResource frontierNode,
        HashSet<string> visitedNodeIds,
        int depth,
        CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();
        var nextFrontier = new List<FocalResource>();
        var unsafeCount = 0;
        var frontierNodeId = ResourceNode.BuildNodeId(frontierNode.EnvironmentId, frontierNode.ResourceType, frontierNode.ResourceId);

        foreach (var source in _sources)
        {
            var sourceResult = await GetFilteredSourceEdgesAsync(source, focal, filters, frontierNode, ct);
            edges.AddRange(sourceResult.Edges);
            unsafeCount += sourceResult.UnsafeCount;
            AddNextFrontier(focal, filters, sourceResult.Edges, visitedNodeIds, nextFrontier, depth, frontierNodeId);
        }

        return (edges, nextFrontier, unsafeCount);
    }

    private async Task<(List<RelationshipEdge> Edges, int UnsafeCount)> GetFilteredSourceEdgesAsync(
        IRelationshipSource source,
        FocalResource focal,
        MapFilter filters,
        FocalResource frontierNode,
        CancellationToken ct)
    {
        IReadOnlyList<RelationshipEdge> sourceEdges;
        try
        {
            sourceEdges = await source.GetEdgesForAsync(frontierNode, filters, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogSourceFailed(focal.EnvironmentId, source.SourceModule);
            return ([], 0);
        }

        LogSourceSucceeded(focal.EnvironmentId, source.SourceModule, sourceEdges.Count);

        var edges = new List<RelationshipEdge>();
        var unsafeCount = 0;
        foreach (var edge in sourceEdges)
        {
            if (edge.EnvironmentId != focal.EnvironmentId)
            {
                unsafeCount++;
                LogRelationshipOmitted(focal.EnvironmentId, source.SourceModule, "CrossEnvironment", edge.RelationshipType);
                continue;
            }

            if (RelationshipEdgeFilter.PassesFilters(edge, filters))
            {
                edges.Add(edge);
            }
        }

        return (edges, unsafeCount);
    }

    private static void AddNextFrontier(
        FocalResource focal,
        MapFilter filters,
        IEnumerable<RelationshipEdge> edges,
        HashSet<string> visitedNodeIds,
        List<FocalResource> nextFrontier,
        int depth,
        string frontierNodeId)
    {
        if (depth + 1 >= filters.Depth)
        {
            return;
        }

        foreach (var edge in edges)
        {
            var neighborNodeId = RelationshipGraphBounding.GetNeighborNodeId(edge, frontierNodeId);
            if (visitedNodeIds.Add(neighborNodeId) && TryCreateFocalResource(focal.EnvironmentId, neighborNodeId, out var resource))
            {
                nextFrontier.Add(resource);
            }
        }
    }

    private static bool TryCreateFocalResource(Guid environmentId, string nodeId, out FocalResource resource)
    {
        var parts = nodeId.Split(':', 3);
        if (parts.Length == 3 && Enum.TryParse<ResourceType>(parts[1], ignoreCase: true, out var resourceType))
        {
            resource = new FocalResource(environmentId, resourceType, parts[2], parts[2], null);
            return true;
        }

        resource = default!;
        return false;
    }

    private async Task<Dictionary<string, ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds,
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct)
    {
        // Materialize once so each source can enumerate the same node ids safely while running concurrently.
        var nodeIdList = nodeIds as IReadOnlyList<string> ?? [.. nodeIds];
        var focalNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, focal.ResourceType, focal.ResourceId);

        // Sources resolve independently, so fan out the (typically network-bound) calls in parallel.
        var perSourceNodes = await Task.WhenAll(
            _sources.Select(source => ResolveNodesFromSourceAsync(source, nodeIdList, focal.EnvironmentId, ct)));

        var result = new Dictionary<string, ResourceNode>();

        // Aggregate in source order to preserve first-wins resolution between overlapping sources.
        foreach (var nodes in perSourceNodes)
        {
            foreach (var node in nodes)
            {
                // Apply health state filter
                if (filters.HealthStates is { Count: > 0 } && !filters.HealthStates.Contains(node.Status))
                    continue;

                var isFocal = node.NodeId == focalNodeId;
                result.TryAdd(node.NodeId, node with { IsFocal = isFocal });
            }
        }

        return result;
    }

    private async Task<IReadOnlyList<ResourceNode>> ResolveNodesFromSourceAsync(
        IRelationshipSource source,
        IReadOnlyList<string> nodeIds,
        Guid environmentId,
        CancellationToken ct)
    {
        try
        {
            return await source.ResolveNodesAsync(nodeIds, environmentId, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogNodeResolutionFailed(environmentId, source.SourceModule);
            return [];
        }
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

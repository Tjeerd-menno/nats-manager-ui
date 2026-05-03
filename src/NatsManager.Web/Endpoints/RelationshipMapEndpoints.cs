using FluentValidation;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Queries;
using NatsManager.Infrastructure.Relationships;

namespace NatsManager.Web.Endpoints;

public static partial class RelationshipMapEndpoints
{
    private static bool TryParseResourceType(string value, out ResourceType resourceType)
    {
        if (value.Equals("Object", StringComparison.OrdinalIgnoreCase))
        {
            resourceType = ResourceType.ObjectStoreObject;
            return true;
        }

        if (value.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
        {
            resourceType = ResourceType.ServiceEndpoint;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out resourceType);
    }

    public static IEndpointRouteBuilder MapRelationshipMapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments")
            .WithTags("RelationshipMap")
            .RequireAuthorization();

        group.MapGet("/{environmentId:guid}/relationships", GetRelationshipMap);
        group.MapGet("/{environmentId:guid}/relationships/map", GetRelationshipMap);
        group.MapGet("/{environmentId:guid}/relationships/nodes/{nodeId}", GetRelationshipNode);

        return app;
    }

    /// <summary>
    /// GET /api/environments/{environmentId}/relationships/map?resourceType=Stream&amp;resourceId=my-stream&amp;depth=1&amp;maxNodes=100&amp;maxEdges=500
    /// </summary>
    private static async Task<IResult> GetRelationshipMap(
        Guid environmentId,
        string? resourceType,
        string? resourceId,
        string? type,
        string? id,
        int depth = 1,
        int maxNodes = 100,
        int maxEdges = 500,
        string? minimumConfidence = null,
        string? minConfidence = null,
        string? relationshipTypes = null,
        string? resourceTypes = null,
        string? healthStates = null,
        bool includeInferred = true,
        bool includeStale = true,
        HttpContext httpContext = default!,
        ILogger<RelationshipMapEndpointLogCategory> logger = default!,
        GetRelationshipMapQueryHandler handler = default!,
        CancellationToken ct = default)
    {
        var focalType = resourceType ?? type;
        var focalId = resourceId ?? id;
        if (string.IsNullOrWhiteSpace(focalType) || string.IsNullOrWhiteSpace(focalId))
            return Results.BadRequest(new { error = "resourceType and resourceId are required." });

        if (!TryParseResourceType(focalType, out var parsedResourceType))
            return Results.BadRequest(new { error = $"Unknown resource type: '{focalType}'. Valid values: {string.Join(", ", Enum.GetNames<ResourceType>())}" });

        LogMapRequestReceived(
            logger,
            environmentId,
            httpContext.TraceIdentifier,
            parsedResourceType,
            depth,
            maxNodes,
            maxEdges);

        // Parse optional confidence filter
        RelationshipConfidence? confidenceFilter = null;
        var confidence = minimumConfidence ?? minConfidence;
        if (!string.IsNullOrEmpty(confidence))
        {
            if (!Enum.TryParse<RelationshipConfidence>(confidence, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = $"Unknown confidence: '{confidence}'." });
            confidenceFilter = parsed;
        }

        // Parse optional relationship type filter
        IReadOnlyList<RelationshipType>? relTypeFilter = null;
        if (!string.IsNullOrEmpty(relationshipTypes))
        {
            var parsed = new List<RelationshipType>();
            foreach (var part in relationshipTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<RelationshipType>(part, ignoreCase: true, out var rel))
                    return Results.BadRequest(new { error = $"Unknown relationship type: '{part}'." });
                parsed.Add(rel);
            }
            relTypeFilter = parsed;
        }

        // Parse optional resource type filter
        IReadOnlyList<ResourceType>? resTypeFilter = null;
        if (!string.IsNullOrEmpty(resourceTypes))
        {
            var parsed = new List<ResourceType>();
            foreach (var part in resourceTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TryParseResourceType(part, out var res))
                    return Results.BadRequest(new { error = $"Unknown resource type: '{part}'." });
                parsed.Add(res);
            }
            resTypeFilter = parsed;
        }

        IReadOnlyList<ResourceHealthStatus>? healthStateFilter = null;
        if (!string.IsNullOrEmpty(healthStates))
        {
            var parsed = new List<ResourceHealthStatus>();
            foreach (var part in healthStates.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Enum.TryParse<ResourceHealthStatus>(part, ignoreCase: true, out var health))
                    return Results.BadRequest(new { error = $"Unknown health state: '{part}'." });
                parsed.Add(health);
            }
            healthStateFilter = parsed;
        }

        var filter = new MapFilter(
            Depth: depth,
            ResourceTypes: resTypeFilter,
            RelationshipTypes: relTypeFilter,
            HealthStates: healthStateFilter,
            MinimumConfidence: confidenceFilter ?? RelationshipConfidence.Low,
            IncludeInferred: includeInferred,
            IncludeStale: includeStale,
            MaxNodes: maxNodes,
            MaxEdges: maxEdges);

        var validator = new MapFilterValidator();
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
        {
            LogMapRequestRejected(
                logger,
                environmentId,
                httpContext.TraceIdentifier,
                parsedResourceType,
                "InvalidFilter");
            return Results.BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var query = new GetRelationshipMapQuery(environmentId, parsedResourceType, focalId, filter);
        var result = await handler.HandleAsync(query, ct);

        if (result.IsNotFound)
        {
            LogMapRequestRejected(
                logger,
                environmentId,
                httpContext.TraceIdentifier,
                parsedResourceType,
                "FocalNotFound");
            return Results.NotFound(new { error = result.NotFoundReason });
        }

        LogMapRequestCompleted(
            logger,
            environmentId,
            httpContext.TraceIdentifier,
            parsedResourceType,
            result.Map?.Nodes.Count ?? 0,
            result.Map?.Edges.Count ?? 0,
            result.Map?.OmittedCounts.UnsafeRelationships ?? 0);
        return Results.Ok(result.Map);
    }

    private static async Task<IResult> GetRelationshipNode(
        Guid environmentId,
        string nodeId,
        HttpContext httpContext,
        ILogger<RelationshipMapEndpointLogCategory> logger,
        GetRelationshipNodeQueryHandler handler = default!,
        CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(environmentId, nodeId), ct);
        if (result.IsInvalid)
        {
            LogNodeRequestRejected(logger, environmentId, httpContext.TraceIdentifier, "InvalidNodeId");
            return Results.BadRequest(new { error = result.ValidationError });
        }

        if (result.IsNotFound || result.Node is null)
        {
            LogNodeRequestRejected(logger, environmentId, httpContext.TraceIdentifier, "NodeNotFound");
            return Results.NotFound(new { error = result.NotFoundReason ?? "Node was not found." });
        }

        LogNodeRequestReceived(logger, environmentId, httpContext.TraceIdentifier, result.Node.ResourceType);
        return Results.Ok(result.Node);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Relationship map request received for environment {EnvironmentId}, correlation {CorrelationId}, resource type {ResourceType}, depth {Depth}, max nodes {MaxNodes}, max edges {MaxEdges}.")]
    private static partial void LogMapRequestReceived(ILogger logger, Guid environmentId, string correlationId, ResourceType resourceType, int depth, int maxNodes, int maxEdges);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Relationship map request rejected for environment {EnvironmentId}, correlation {CorrelationId}, resource type {ResourceType}, reason {Reason}.")]
    private static partial void LogMapRequestRejected(ILogger logger, Guid environmentId, string correlationId, ResourceType resourceType, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Relationship map request completed for environment {EnvironmentId}, correlation {CorrelationId}, resource type {ResourceType}: {NodeCount} node(s), {EdgeCount} edge(s), unsafe relationships {UnsafeRelationships}.")]
    private static partial void LogMapRequestCompleted(ILogger logger, Guid environmentId, string correlationId, ResourceType resourceType, int nodeCount, int edgeCount, int unsafeRelationships);

    [LoggerMessage(Level = LogLevel.Information, Message = "Relationship node request received for environment {EnvironmentId}, correlation {CorrelationId}, resource type {ResourceType}.")]
    private static partial void LogNodeRequestReceived(ILogger logger, Guid environmentId, string correlationId, ResourceType resourceType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Relationship node request rejected for environment {EnvironmentId}, correlation {CorrelationId}, reason {Reason}.")]
    private static partial void LogNodeRequestRejected(ILogger logger, Guid environmentId, string correlationId, string reason);

    private sealed class RelationshipMapEndpointLogCategory;
}

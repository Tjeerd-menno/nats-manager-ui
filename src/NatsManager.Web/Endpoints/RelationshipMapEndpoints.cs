using Microsoft.AspNetCore.Mvc;
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
        [AsParameters] RelationshipMapRequest request,
        HttpContext httpContext,
        ILogger<RelationshipMapEndpointLogCategory> logger,
        GetRelationshipMapQueryHandler handler,
        CancellationToken ct = default)
    {
        var focalType = request.ResourceType ?? request.Type;
        var focalId = request.ResourceId ?? request.Id;
        if (string.IsNullOrWhiteSpace(focalType))
            return ApiProblemResults.ValidationProblem("resourceType", "resourceType is required.");
        if (string.IsNullOrWhiteSpace(focalId))
            return ApiProblemResults.ValidationProblem("resourceId", "resourceId is required.");

        if (!TryParseResourceType(focalType, out var parsedResourceType))
            return ApiProblemResults.ValidationProblem("resourceType", $"Unknown resource type: '{focalType}'. Valid values: {string.Join(", ", Enum.GetNames<ResourceType>())}");

        LogMapRequestReceived(
            logger,
            request.EnvironmentId,
            httpContext.TraceIdentifier,
            parsedResourceType,
            request.Depth,
            request.MaxNodes,
            request.MaxEdges);

        var filterResult = TryCreateFilter(request);
        if (filterResult.ValidationError is not null)
        {
            return filterResult.ValidationError;
        }

        var validator = new MapFilterValidator();
        var validation = await validator.ValidateAsync(filterResult.Filter, ct);
        if (!validation.IsValid)
        {
            LogMapRequestRejected(
                logger,
                request.EnvironmentId,
                httpContext.TraceIdentifier,
                parsedResourceType,
                "InvalidFilter");
            return ApiProblemResults.ValidationProblem(validation.Errors);
        }

        var query = new GetRelationshipMapQuery(request.EnvironmentId, parsedResourceType, focalId, filterResult.Filter);
        var result = await handler.HandleAsync(query, ct);

        if (result.IsNotFound)
        {
            LogMapRequestRejected(
                logger,
                request.EnvironmentId,
                httpContext.TraceIdentifier,
                parsedResourceType,
                "FocalNotFound");
            return ApiProblemResults.NotFound(result.NotFoundReason ?? "Resource was not found.");
        }

        LogMapRequestCompleted(
            logger,
            request.EnvironmentId,
            httpContext.TraceIdentifier,
            parsedResourceType,
            result.Map?.Nodes.Count ?? 0,
            result.Map?.Edges.Count ?? 0,
            result.Map?.OmittedCounts.UnsafeRelationships ?? 0);
        return Results.Ok(result.Map);
    }

    private static (MapFilter Filter, IResult? ValidationError) TryCreateFilter(RelationshipMapRequest request)
    {
        var confidenceResult = TryParseConfidence(request.MinimumConfidence ?? request.MinConfidence);
        if (confidenceResult.ValidationError is not null)
        {
            return (default!, confidenceResult.ValidationError);
        }

        var relationshipTypesResult = TryParseEnumList<RelationshipType>(request.RelationshipTypes, "relationshipTypes");
        if (relationshipTypesResult.ValidationError is not null)
        {
            return (default!, relationshipTypesResult.ValidationError);
        }

        var resourceTypesResult = TryParseResourceTypeList(request.ResourceTypes);
        if (resourceTypesResult.ValidationError is not null)
        {
            return (default!, resourceTypesResult.ValidationError);
        }

        var healthStatesResult = TryParseEnumList<ResourceHealthStatus>(request.HealthStates, "healthStates");
        if (healthStatesResult.ValidationError is not null)
        {
            return (default!, healthStatesResult.ValidationError);
        }

        return (new MapFilter(
            Depth: request.Depth,
            ResourceTypes: resourceTypesResult.Values,
            RelationshipTypes: relationshipTypesResult.Values,
            HealthStates: healthStatesResult.Values,
            MinimumConfidence: confidenceResult.Value ?? RelationshipConfidence.Low,
            IncludeInferred: request.IncludeInferred,
            IncludeStale: request.IncludeStale,
            MaxNodes: request.MaxNodes,
            MaxEdges: request.MaxEdges), null);
    }

    private static (RelationshipConfidence? Value, IResult? ValidationError) TryParseConfidence(string? confidence)
    {
        if (string.IsNullOrEmpty(confidence))
        {
            return (null, null);
        }

        if (Enum.TryParse<RelationshipConfidence>(confidence, ignoreCase: true, out var parsed))
        {
            return (parsed, null);
        }

        return (null, ApiProblemResults.ValidationProblem("minimumConfidence", $"Unknown confidence: '{confidence}'."));
    }

    private static (IReadOnlyList<TEnum>? Values, IResult? ValidationError) TryParseEnumList<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            return (null, null);
        }

        var parsed = new List<TEnum>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<TEnum>(part, ignoreCase: true, out var item))
            {
                return (null, ApiProblemResults.ValidationProblem(parameterName, $"Unknown {parameterName}: '{part}'."));
            }

            parsed.Add(item);
        }

        return (parsed, null);
    }

    private static (IReadOnlyList<ResourceType>? Values, IResult? ValidationError) TryParseResourceTypeList(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return (null, null);
        }

        var parsed = new List<ResourceType>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseResourceType(part, out var resourceType))
            {
                return (null, ApiProblemResults.ValidationProblem("resourceTypes", $"Unknown resource type: '{part}'."));
            }

            parsed.Add(resourceType);
        }

        return (parsed, null);
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
            LogNodeRequestRejected(logger, environmentId, httpContext.TraceIdentifier, GetNodeRejectionReason(result));
            return ApiProblemResults.ValidationProblem("nodeId", result.ValidationError ?? "Node id is invalid.");
        }

        if (result.IsNotFound || result.Node is null)
        {
            LogNodeRequestRejected(logger, environmentId, httpContext.TraceIdentifier, GetNodeRejectionReason(result));
            return ApiProblemResults.NotFound(result.NotFoundReason ?? "Node was not found.");
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

    private static string GetNodeRejectionReason(RelationshipNodeResult result) =>
        result.RejectionReason?.ToString() ?? "Unknown";

    private sealed class RelationshipMapEndpointLogCategory;
}

public sealed record RelationshipMapRequest
{
    [FromRoute] public Guid EnvironmentId { get; init; }
    [FromQuery] public string? ResourceType { get; init; }
    [FromQuery] public string? ResourceId { get; init; }
    [FromQuery] public string? Type { get; init; }
    [FromQuery] public string? Id { get; init; }
    [FromQuery] public int Depth { get; init; } = 1;
    [FromQuery] public int MaxNodes { get; init; } = 100;
    [FromQuery] public int MaxEdges { get; init; } = 500;
    [FromQuery] public string? MinimumConfidence { get; init; }
    [FromQuery] public string? MinConfidence { get; init; }
    [FromQuery] public string? RelationshipTypes { get; init; }
    [FromQuery] public string? ResourceTypes { get; init; }
    [FromQuery] public string? HealthStates { get; init; }
    [FromQuery] public bool IncludeInferred { get; init; } = true;
    [FromQuery] public bool IncludeStale { get; init; } = true;
}

using FluentValidation;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Queries;
using NatsManager.Infrastructure.Relationships;

namespace NatsManager.Web.Endpoints;

public static class RelationshipMapEndpoints
{
    public static IEndpointRouteBuilder MapRelationshipMapEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments")
            .WithTags("RelationshipMap")
            .RequireAuthorization();

        group.MapGet("/{environmentId:guid}/relationships", GetRelationshipMap);

        return app;
    }

    /// <summary>
    /// GET /api/environments/{environmentId}/relationships?type=Stream&amp;id=my-stream&amp;depth=2&amp;maxNodes=100&amp;maxEdges=500
    /// </summary>
    private static async Task<IResult> GetRelationshipMap(
        Guid environmentId,
        string type,
        string id,
        int depth = 2,
        int maxNodes = 100,
        int maxEdges = 500,
        string? minConfidence = null,
        string? relationshipTypes = null,
        string? resourceTypes = null,
        GetRelationshipMapQueryHandler handler = default!,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<ResourceType>(type, ignoreCase: true, out var resourceType))
            return Results.BadRequest(new { error = $"Unknown resource type: '{type}'. Valid values: {string.Join(", ", Enum.GetNames<ResourceType>())}" });

        // Parse optional confidence filter
        RelationshipConfidence? confidenceFilter = null;
        if (!string.IsNullOrEmpty(minConfidence))
        {
            if (!Enum.TryParse<RelationshipConfidence>(minConfidence, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = $"Unknown confidence: '{minConfidence}'." });
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
                if (!Enum.TryParse<ResourceType>(part, ignoreCase: true, out var res))
                    return Results.BadRequest(new { error = $"Unknown resource type: '{part}'." });
                parsed.Add(res);
            }
            resTypeFilter = parsed;
        }

        var filter = new MapFilter(
            Depth: depth,
            MaxNodes: maxNodes,
            MaxEdges: maxEdges,
            MinimumConfidence: confidenceFilter ?? RelationshipConfidence.Low,
            RelationshipTypes: relTypeFilter,
            ResourceTypes: resTypeFilter,
            IncludeInferred: true);

        var validator = new MapFilterValidator();
        var validation = await validator.ValidateAsync(filter, ct);
        if (!validation.IsValid)
            return Results.BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var query = new GetRelationshipMapQuery(environmentId, resourceType, id, filter);
        var result = await handler.HandleAsync(query, ct);

        if (result.IsNotFound)
            return Results.NotFound(new { error = result.NotFoundReason });

        return Results.Ok(result.Map);
    }
}

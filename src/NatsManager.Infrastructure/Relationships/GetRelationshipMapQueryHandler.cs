using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Application.Modules.Relationships.Queries;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Handles GetRelationshipMapQuery by orchestrating focal resource resolution
/// and relationship projection. Lives in Infrastructure per Clean Architecture rules.
/// </summary>
public sealed class GetRelationshipMapQueryHandler(
    IFocalResourceResolver focalResourceResolver,
    RelationshipProjectionService projectionService)
{
    public async Task<RelationshipMapResult> HandleAsync(
        GetRelationshipMapQuery query,
        CancellationToken ct = default)
    {
        var focal = await focalResourceResolver.ResolveAsync(
            query.EnvironmentId, query.ResourceType, query.ResourceId, ct);

        if (focal == null)
            return new RelationshipMapResult(null, NotFoundReason: $"Resource '{query.ResourceType}:{query.ResourceId}' not found in environment {query.EnvironmentId}.");

        var map = await projectionService.ProjectAsync(focal, query.Filters, ct);
        return new RelationshipMapResult(map, NotFoundReason: null);
    }
}

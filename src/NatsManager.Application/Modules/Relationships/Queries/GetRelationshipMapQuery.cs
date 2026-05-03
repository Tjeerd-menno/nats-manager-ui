using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Application.Modules.Relationships.Queries;

/// <summary>Query to build and return the relationship map for a focal resource.</summary>
public sealed record GetRelationshipMapQuery(
    Guid EnvironmentId,
    ResourceType ResourceType,
    string ResourceId,
    MapFilter Filters);

/// <summary>Result wrapper that distinguishes between not-found and success.</summary>
public sealed record RelationshipMapResult(
    RelationshipMap? Map,
    string? NotFoundReason)
{
    public bool IsNotFound => Map == null;
}

namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>
/// Response root for the relationship map endpoint.
/// </summary>
public sealed record RelationshipMap(
    Guid EnvironmentId,
    FocalResource FocalResource,
    DateTimeOffset GeneratedAt,
    int Depth,
    IReadOnlyList<ResourceNode> Nodes,
    IReadOnlyList<RelationshipEdge> Edges,
    MapFilter Filters,
    OmittedCounts OmittedCounts);

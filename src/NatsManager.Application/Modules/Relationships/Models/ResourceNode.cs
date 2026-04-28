namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>
/// A visible node in the relationship graph.
/// NodeId format: {environment}:{type}:{id}
/// Metadata excludes payload, credentials, JWTs.
/// </summary>
public sealed record ResourceNode(
    string NodeId,
    Guid EnvironmentId,
    ResourceType ResourceType,
    string ResourceId,
    string DisplayName,
    ResourceHealthStatus Status,
    RelationshipFreshness Freshness,
    bool IsFocal,
    string? DetailRoute,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static string BuildNodeId(Guid environmentId, ResourceType resourceType, string resourceId) =>
        $"{environmentId}:{resourceType.ToString().ToLowerInvariant()}:{resourceId}";
}

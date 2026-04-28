namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>The focal resource around which the relationship map is centered.</summary>
public sealed record FocalResource(
    Guid EnvironmentId,
    ResourceType ResourceType,
    string ResourceId,
    string DisplayName,
    string? Route);

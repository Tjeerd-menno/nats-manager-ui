using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Application.Modules.Relationships.Ports;

/// <summary>
/// Resolves a focal resource to determine its existence, display name, and detail route.
/// Returns null for missing focal resources (drives 404 response).
/// </summary>
public interface IFocalResourceResolver
{
    Task<FocalResource?> ResolveAsync(
        Guid environmentId,
        ResourceType resourceType,
        string resourceId,
        CancellationToken ct);
}

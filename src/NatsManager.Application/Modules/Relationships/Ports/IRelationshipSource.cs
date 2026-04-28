using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Application.Modules.Relationships.Ports;

/// <summary>
/// Source of relationship edges for a specific module (JetStream, KV, etc.).
/// Implementations must not duplicate resource ownership — they read from existing adapters.
/// </summary>
public interface IRelationshipSource
{
    RelationshipSourceModule SourceModule { get; }

    /// <summary>Returns edges connected to or from the focal resource.</summary>
    Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct);

    /// <summary>Resolves nodes by their deterministic node IDs.</summary>
    Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds,
        Guid environmentId,
        CancellationToken ct);
}

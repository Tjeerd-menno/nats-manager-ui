using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>
/// Alerts relationship source. Currently a stub — alerts module is not yet implemented.
/// Returns no edges in the current version.
/// </summary>
public sealed class AlertsRelationshipSource : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.Alerts;

    public Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<RelationshipEdge>>([]);

    public Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ResourceNode>>([]);
}

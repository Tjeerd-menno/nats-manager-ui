using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Application.Modules.Relationships.Queries;

namespace NatsManager.Infrastructure.Relationships;

public sealed class GetRelationshipNodeQueryHandler(
    IFocalResourceResolver focalResourceResolver,
    RelationshipProjectionService projectionService)
{
    public async Task<RelationshipNodeResult> HandleAsync(
        GetRelationshipNodeQuery query,
        CancellationToken ct = default)
    {
        var prefix = $"{query.EnvironmentId}:";
        if (!query.NodeId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return new RelationshipNodeResult(null, "Node was not found in this environment.", null);

        var remainder = query.NodeId[prefix.Length..];
        var separator = remainder.IndexOf(':');
        if (separator <= 0 || separator == remainder.Length - 1)
            return new RelationshipNodeResult(null, null, "Invalid node id.");

        var typeName = remainder[..separator];
        var resourceId = remainder[(separator + 1)..];
        if (!Enum.TryParse<ResourceType>(typeName, ignoreCase: true, out var resourceType))
            return new RelationshipNodeResult(null, null, $"Unknown resource type in node id: '{typeName}'.");

        var focal = await focalResourceResolver.ResolveAsync(
            query.EnvironmentId,
            resourceType,
            resourceId,
            ct);
        if (focal is null)
            return new RelationshipNodeResult(null, $"Resource '{resourceType}:{resourceId}' not found in environment {query.EnvironmentId}.", null);

        var map = await projectionService.ProjectAsync(focal, MapFilter.Default, ct);
        var node = map.Nodes.FirstOrDefault(n => n.NodeId == query.NodeId);

        return new RelationshipNodeResult(
            new RelationshipNodeDetails(
                query.NodeId,
                focal.ResourceType,
                focal.ResourceId,
                focal.DisplayName,
                node?.Status ?? ResourceHealthStatus.Unknown,
                node?.Freshness ?? RelationshipFreshness.Unavailable,
                focal.Route,
                CanRecenter: true),
            null,
            null);
    }
}

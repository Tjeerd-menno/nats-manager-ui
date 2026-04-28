using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Application.Modules.Services.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>Services relationship source: service→subject endpoints, service→queue groups.</summary>
public sealed class ServicesRelationshipSource(IServiceDiscoveryAdapter serviceDiscoveryAdapter) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.Services;

    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType == ResourceType.Service)
        {
            var service = await serviceDiscoveryAdapter.GetServiceAsync(focal.EnvironmentId, focal.ResourceId, ct);
            if (service == null) return edges;

            var serviceNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Service, focal.ResourceId);

            foreach (var endpoint in service.Endpoints)
            {
                var subjectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Subject, endpoint.Subject);
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.Services,
                    EvidenceType: "ServiceEndpointSubject",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Service {focal.ResourceId} exposes endpoint on subject {endpoint.Subject}",
                    SafeFields: new Dictionary<string, string>
                    {
                        ["service"] = focal.ResourceId,
                        ["endpoint"] = endpoint.Name,
                        ["subject"] = endpoint.Subject
                    });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(serviceNodeId, subjectNodeId, RelationshipType.UsesSubject, RelationshipSourceModule.Services),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: serviceNodeId,
                    TargetNodeId: subjectNodeId,
                    RelationshipType: RelationshipType.UsesSubject,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Observed,
                    Confidence: RelationshipConfidence.High,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [evidence]));
            }
        }
        else if (focal.ResourceType == ResourceType.Subject)
        {
            // Find services whose endpoints match this subject
            var services = await serviceDiscoveryAdapter.DiscoverServicesAsync(focal.EnvironmentId, ct);
            foreach (var svc in services)
            {
                foreach (var ep in svc.Endpoints)
                {
                    if (ep.Subject != focal.ResourceId) continue;

                    var serviceNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Service, svc.Name);
                    var subjectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Subject, focal.ResourceId);
                    var evidence = new RelationshipEvidence(
                        SourceModule: RelationshipSourceModule.Services,
                        EvidenceType: "ServiceEndpointSubject",
                        ObservedAt: DateTimeOffset.UtcNow,
                        Freshness: RelationshipFreshness.Live,
                        Summary: $"Service {svc.Name} listens on subject {focal.ResourceId}",
                        SafeFields: new Dictionary<string, string> { ["service"] = svc.Name, ["subject"] = focal.ResourceId });

                    edges.Add(new RelationshipEdge(
                        EdgeId: RelationshipEdge.BuildEdgeId(serviceNodeId, subjectNodeId, RelationshipType.UsesSubject, RelationshipSourceModule.Services),
                        EnvironmentId: focal.EnvironmentId,
                        SourceNodeId: serviceNodeId,
                        TargetNodeId: subjectNodeId,
                        RelationshipType: RelationshipType.UsesSubject,
                        Direction: RelationshipDirection.Inbound,
                        ObservationKind: ObservationKind.Observed,
                        Confidence: RelationshipConfidence.High,
                        Freshness: RelationshipFreshness.Live,
                        Status: ResourceHealthStatus.Healthy,
                        Evidence: [evidence]));
                }
            }
        }

        return edges;
    }

    public async Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)
    {
        var nodes = new List<ResourceNode>();
        var services = await serviceDiscoveryAdapter.DiscoverServicesAsync(environmentId, ct);
        var serviceMap = services.ToDictionary(s => s.Name);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "service" && serviceMap.TryGetValue(parts[2], out var svc))
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.Service,
                    ResourceId: parts[2],
                    DisplayName: svc.Name,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string> { ["version"] = svc.Version }));
            }
        }

        return nodes;
    }
}

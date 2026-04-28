using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>Core NATS relationship source: server→subject, client→subject.</summary>
public sealed class CoreNatsRelationshipSource(ICoreNatsAdapter coreNatsAdapter) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.CoreNats;

    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType == ResourceType.Subject)
        {
            // Subject → clients that have subscriptions (inferred)
            var clients = await coreNatsAdapter.ListClientsAsync(focal.EnvironmentId, ct);
            var subjectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Subject, focal.ResourceId);
            var serverInfo = await coreNatsAdapter.GetServerInfoAsync(focal.EnvironmentId, ct);
            if (serverInfo != null)
            {
                var serverNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Server, serverInfo.ServerId);
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.CoreNats,
                    EvidenceType: "SubjectOnServer",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Subject {focal.ResourceId} routes through server {serverInfo.ServerName}",
                    SafeFields: new Dictionary<string, string>
                    {
                        ["subject"] = focal.ResourceId,
                        ["server"] = serverInfo.ServerName
                    });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(subjectNodeId, serverNodeId, RelationshipType.RoutedThrough, RelationshipSourceModule.CoreNats),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: subjectNodeId,
                    TargetNodeId: serverNodeId,
                    RelationshipType: RelationshipType.RoutedThrough,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Inferred,
                    Confidence: RelationshipConfidence.Medium,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [evidence]));
            }

            // Connected clients → subject (inferred)
            foreach (var client in clients.Take(10))
            {
                var clientNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Client, client.Id.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var clientEvidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.CoreNats,
                    EvidenceType: "ClientConnected",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Client {client.Name} may interact with subject {focal.ResourceId}",
                    SafeFields: new Dictionary<string, string>
                    {
                        ["client"] = client.Name ?? client.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["subject"] = focal.ResourceId
                    });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(clientNodeId, subjectNodeId, RelationshipType.PublishesTo, RelationshipSourceModule.CoreNats),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: clientNodeId,
                    TargetNodeId: subjectNodeId,
                    RelationshipType: RelationshipType.PublishesTo,
                    Direction: RelationshipDirection.Inbound,
                    ObservationKind: ObservationKind.Inferred,
                    Confidence: RelationshipConfidence.Low,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Unknown,
                    Evidence: [clientEvidence]));
            }
        }
        else if (focal.ResourceType == ResourceType.Server)
        {
            // Server → subjects hosted
            var subjects = await coreNatsAdapter.ListSubjectsAsync(focal.EnvironmentId, ct);
            var serverNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Server, focal.ResourceId);
            foreach (var sub in subjects.Take(20))
            {
                var subjectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Subject, sub.Subject);
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.CoreNats,
                    EvidenceType: "SubjectOnServer",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Server hosts subject {sub.Subject} with {sub.Subscriptions} subscription(s)",
                    SafeFields: new Dictionary<string, string>
                    {
                        ["subject"] = sub.Subject,
                        ["subscriptions"] = sub.Subscriptions.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(serverNodeId, subjectNodeId, RelationshipType.RoutedThrough, RelationshipSourceModule.CoreNats),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: serverNodeId,
                    TargetNodeId: subjectNodeId,
                    RelationshipType: RelationshipType.RoutedThrough,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Observed,
                    Confidence: RelationshipConfidence.High,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [evidence]));
            }
        }

        return edges;
    }

    public async Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)
    {
        var nodes = new List<ResourceNode>();
        var serverInfo = await coreNatsAdapter.GetServerInfoAsync(environmentId, ct);
        var subjects = await coreNatsAdapter.ListSubjectsAsync(environmentId, ct);
        var subjectMap = subjects.ToDictionary(s => s.Subject);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "server" && serverInfo != null && serverInfo.ServerId == parts[2])
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.Server,
                    ResourceId: parts[2],
                    DisplayName: serverInfo.ServerName,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>
                    {
                        ["version"] = serverInfo.Version,
                        ["connections"] = serverInfo.Connections.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }));
            }
            else if (parts[1] == "subject" && subjectMap.TryGetValue(parts[2], out var sub))
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.Subject,
                    ResourceId: parts[2],
                    DisplayName: sub.Subject,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>
                    {
                        ["subscriptions"] = sub.Subscriptions.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }));
            }
        }

        return nodes;
    }
}

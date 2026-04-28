using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>
/// Monitoring relationship source: surfaces server→health status edges
/// derived from the monitoring metrics store.
/// </summary>
public sealed class MonitoringRelationshipSource(IMonitoringMetricsStore metricsStore) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.Monitoring;

    public Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType != ResourceType.Server) return Task.FromResult<IReadOnlyList<RelationshipEdge>>(edges);

        var latest = metricsStore.GetLatest(focal.EnvironmentId);
        if (latest == null) return Task.FromResult<IReadOnlyList<RelationshipEdge>>(edges);

        var serverNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Server, focal.ResourceId);

        // Server → JetStream (if enabled)
        if (latest.JetStream != null)
        {
            var jsNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.JetStreamAccount, "jetstream");
            var evidence = new RelationshipEvidence(
                SourceModule: RelationshipSourceModule.Monitoring,
                EvidenceType: "JetStreamEnabled",
                ObservedAt: latest.Timestamp,
                Freshness: RelationshipFreshness.Live,
                Summary: $"JetStream enabled on server with {latest.JetStream.StreamCount} stream(s)",
                SafeFields: new Dictionary<string, string>
                {
                    ["streams"] = latest.JetStream.StreamCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["consumers"] = latest.JetStream.ConsumerCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

            edges.Add(new RelationshipEdge(
                EdgeId: RelationshipEdge.BuildEdgeId(serverNodeId, jsNodeId, RelationshipType.HostsJetStream, RelationshipSourceModule.Monitoring),
                EnvironmentId: focal.EnvironmentId,
                SourceNodeId: serverNodeId,
                TargetNodeId: jsNodeId,
                RelationshipType: RelationshipType.HostsJetStream,
                Direction: RelationshipDirection.Outbound,
                ObservationKind: ObservationKind.Observed,
                Confidence: RelationshipConfidence.High,
                Freshness: RelationshipFreshness.Live,
                Status: latest.HealthStatus == Application.Modules.Monitoring.Models.MonitoringStatus.Ok
                    ? ResourceHealthStatus.Healthy
                    : ResourceHealthStatus.Degraded,
                Evidence: [evidence]));
        }

        return Task.FromResult<IReadOnlyList<RelationshipEdge>>(edges);
    }

    public Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)
    {
        var nodes = new List<ResourceNode>();
        var latest = metricsStore.GetLatest(environmentId);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "jetsreamaccount" && latest?.JetStream != null)
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.JetStreamAccount,
                    ResourceId: "jetstream",
                    DisplayName: "JetStream",
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>
                    {
                        ["streams"] = latest.JetStream.StreamCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["consumers"] = latest.JetStream.ConsumerCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    }));
            }
        }

        return Task.FromResult<IReadOnlyList<ResourceNode>>(nodes);
    }
}

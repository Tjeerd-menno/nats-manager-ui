using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>
/// Provides relationship edges from JetStream: stream→consumer (Contains),
/// stream→subject (UsesSubject), consumer→stream (BackedByStream).
/// Consumes existing IJetStreamAdapter — does not duplicate ownership.
/// </summary>
public sealed class JetStreamRelationshipSource(IJetStreamAdapter jetStreamAdapter) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.JetStream;

    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal,
        MapFilter filters,
        CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType == ResourceType.Stream)
        {
            // Stream → consumers
            var consumers = await jetStreamAdapter.ListConsumersAsync(focal.EnvironmentId, focal.ResourceId, ct);
            foreach (var consumer in consumers)
            {
                var streamNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Stream, focal.ResourceId);
                var consumerNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Consumer, $"{focal.ResourceId}/{consumer.Name}");
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.JetStream,
                    EvidenceType: "ConsumerParent",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Consumer {consumer.Name} belongs to stream {focal.ResourceId}",
                    SafeFields: new Dictionary<string, string> { ["stream"] = focal.ResourceId, ["consumer"] = consumer.Name });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(streamNodeId, consumerNodeId, RelationshipType.Contains, RelationshipSourceModule.JetStream),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: streamNodeId,
                    TargetNodeId: consumerNodeId,
                    RelationshipType: RelationshipType.Contains,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Observed,
                    Confidence: RelationshipConfidence.High,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [evidence]));
            }

            // Stream → subjects
            var streamConfig = await jetStreamAdapter.GetStreamConfigAsync(focal.EnvironmentId, focal.ResourceId, ct);
            if (streamConfig?.Subjects != null)
            {
                foreach (var subject in streamConfig.Subjects)
                {
                    var streamNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Stream, focal.ResourceId);
                    var subjectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Subject, subject);
                    var evidence = new RelationshipEvidence(
                        SourceModule: RelationshipSourceModule.JetStream,
                        EvidenceType: "StreamSubject",
                        ObservedAt: DateTimeOffset.UtcNow,
                        Freshness: RelationshipFreshness.Live,
                        Summary: $"Stream {focal.ResourceId} captures subject {subject}",
                        SafeFields: new Dictionary<string, string> { ["stream"] = focal.ResourceId, ["subject"] = subject });

                    edges.Add(new RelationshipEdge(
                        EdgeId: RelationshipEdge.BuildEdgeId(streamNodeId, subjectNodeId, RelationshipType.UsesSubject, RelationshipSourceModule.JetStream),
                        EnvironmentId: focal.EnvironmentId,
                        SourceNodeId: streamNodeId,
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
        else if (focal.ResourceType == ResourceType.Consumer)
        {
            // Consumer → backing stream
            var parts = focal.ResourceId.Split('/', 2);
            if (parts.Length == 2)
            {
                var streamName = parts[0];
                var consumerNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Consumer, focal.ResourceId);
                var streamNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Stream, streamName);
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.JetStream,
                    EvidenceType: "ConsumerParent",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Consumer {parts[1]} is backed by stream {streamName}",
                    SafeFields: new Dictionary<string, string> { ["stream"] = streamName, ["consumer"] = parts[1] });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(consumerNodeId, streamNodeId, RelationshipType.BackedByStream, RelationshipSourceModule.JetStream),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: consumerNodeId,
                    TargetNodeId: streamNodeId,
                    RelationshipType: RelationshipType.BackedByStream,
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
        IEnumerable<string> nodeIds,
        Guid environmentId,
        CancellationToken ct)
    {
        var nodes = new List<ResourceNode>();
        var streams = await jetStreamAdapter.ListStreamsAsync(environmentId, ct);
        var streamMap = streams.ToDictionary(s => s.Name);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "stream" && streamMap.TryGetValue(parts[2], out var stream))
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.Stream,
                    ResourceId: parts[2],
                    DisplayName: stream.Name,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>()));
            }
            else if (parts[1] == "consumer")
            {
                var consumerParts = parts[2].Split('/', 2);
                if (consumerParts.Length == 2)
                {
                    var consumer = await jetStreamAdapter.GetConsumerAsync(environmentId, consumerParts[0], consumerParts[1], ct);
                    if (consumer != null)
                    {
                        nodes.Add(new ResourceNode(
                            NodeId: nodeId,
                            EnvironmentId: environmentId,
                            ResourceType: ResourceType.Consumer,
                            ResourceId: parts[2],
                            DisplayName: consumer.Name,
                            Status: ResourceHealthStatus.Healthy,
                            Freshness: RelationshipFreshness.Live,
                            IsFocal: false,
                            DetailRoute: null,
                            Metadata: new Dictionary<string, string>()));
                    }
                }
            }
        }

        return nodes;
    }
}

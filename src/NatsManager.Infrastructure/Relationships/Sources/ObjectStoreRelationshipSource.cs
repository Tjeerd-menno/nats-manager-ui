using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>Object Store relationship source: bucket→backing stream, bucket→objects.</summary>
public sealed class ObjectStoreRelationshipSource(IObjectStoreAdapter objectStoreAdapter) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.ObjectStore;

    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType == ResourceType.ObjectBucket)
        {
            var bucketNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.ObjectBucket, focal.ResourceId);

            // Bucket → backing JetStream stream (OBJ_{name})
            var backingStreamName = $"OBJ_{focal.ResourceId}";
            var streamNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Stream, backingStreamName);
            var evidence = new RelationshipEvidence(
                SourceModule: RelationshipSourceModule.ObjectStore,
                EvidenceType: "ObjectBucketBackingStream",
                ObservedAt: DateTimeOffset.UtcNow,
                Freshness: RelationshipFreshness.Live,
                Summary: $"Object bucket {focal.ResourceId} is backed by JetStream stream {backingStreamName}",
                SafeFields: new Dictionary<string, string> { ["bucket"] = focal.ResourceId, ["stream"] = backingStreamName });

            edges.Add(new RelationshipEdge(
                EdgeId: RelationshipEdge.BuildEdgeId(bucketNodeId, streamNodeId, RelationshipType.BackedByStream, RelationshipSourceModule.ObjectStore),
                EnvironmentId: focal.EnvironmentId,
                SourceNodeId: bucketNodeId,
                TargetNodeId: streamNodeId,
                RelationshipType: RelationshipType.BackedByStream,
                Direction: RelationshipDirection.Outbound,
                ObservationKind: ObservationKind.Observed,
                Confidence: RelationshipConfidence.High,
                Freshness: RelationshipFreshness.Live,
                Status: ResourceHealthStatus.Healthy,
                Evidence: [evidence]));

            // Bucket → objects (sampled)
            var objects = await objectStoreAdapter.ListObjectsAsync(focal.EnvironmentId, focal.ResourceId, ct);
            foreach (var obj in objects.Take(20))
            {
                var objectNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.ObjectStoreObject, $"{focal.ResourceId}/{obj.Name}");
                var objEvidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.ObjectStore,
                    EvidenceType: "ObjectInBucket",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Object {obj.Name} exists in bucket {focal.ResourceId}",
                    SafeFields: new Dictionary<string, string> { ["bucket"] = focal.ResourceId, ["object"] = obj.Name });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(bucketNodeId, objectNodeId, RelationshipType.Contains, RelationshipSourceModule.ObjectStore),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: bucketNodeId,
                    TargetNodeId: objectNodeId,
                    RelationshipType: RelationshipType.Contains,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Observed,
                    Confidence: RelationshipConfidence.High,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [objEvidence]));
            }
        }

        return edges;
    }

    public async Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
        IEnumerable<string> nodeIds, Guid environmentId, CancellationToken ct)
    {
        var nodes = new List<ResourceNode>();
        var buckets = await objectStoreAdapter.ListBucketsAsync(environmentId, ct);
        var bucketMap = buckets.ToDictionary(b => b.BucketName);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "objectbucket" && bucketMap.TryGetValue(parts[2], out var bucket))
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.ObjectBucket,
                    ResourceId: parts[2],
                    DisplayName: bucket.BucketName,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>()));
            }
        }

        return nodes;
    }
}

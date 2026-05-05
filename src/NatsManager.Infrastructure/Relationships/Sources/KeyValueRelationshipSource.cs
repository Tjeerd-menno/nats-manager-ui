using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;

namespace NatsManager.Infrastructure.Relationships.Sources;

/// <summary>KV Store relationship source: bucket→backing stream, key→bucket.</summary>
public sealed class KeyValueRelationshipSource(IKvStoreAdapter kvStoreAdapter) : IRelationshipSource
{
    public RelationshipSourceModule SourceModule => RelationshipSourceModule.KeyValue;

    private const string BucketKey = "bucket";

    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
        FocalResource focal, MapFilter filters, CancellationToken ct)
    {
        var edges = new List<RelationshipEdge>();

        if (focal.ResourceType == ResourceType.KvBucket)
        {
            var bucketNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.KvBucket, focal.ResourceId);
            var backingStreamName = $"KV_{focal.ResourceId}";
            var streamNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.Stream, backingStreamName);

            var evidence = new RelationshipEvidence(
                SourceModule: RelationshipSourceModule.KeyValue,
                EvidenceType: "KvBucketBackingStream",
                ObservedAt: DateTimeOffset.UtcNow,
                Freshness: RelationshipFreshness.Live,
                Summary: $"KV bucket {focal.ResourceId} is backed by JetStream stream {backingStreamName}",
                SafeFields: new Dictionary<string, string> { [BucketKey] = focal.ResourceId, ["stream"] = backingStreamName });

            edges.Add(new RelationshipEdge(
                EdgeId: RelationshipEdge.BuildEdgeId(bucketNodeId, streamNodeId, RelationshipType.BackedByStream, RelationshipSourceModule.KeyValue),
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

            // Bucket → keys (inferred, sampled)
            var keys = await kvStoreAdapter.ListKeysAsync(focal.EnvironmentId, focal.ResourceId, null, ct);
            foreach (var key in keys.Take(20)) // limit for performance
            {
                var keyNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.KvKey, $"{focal.ResourceId}/{key.Key}");
                var keyEvidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.KeyValue,
                    EvidenceType: "KvKeyInBucket",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Key {key.Key} exists in bucket {focal.ResourceId}",
                    SafeFields: new Dictionary<string, string> { [BucketKey] = focal.ResourceId, ["key"] = key.Key });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(bucketNodeId, keyNodeId, RelationshipType.Contains, RelationshipSourceModule.KeyValue),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: bucketNodeId,
                    TargetNodeId: keyNodeId,
                    RelationshipType: RelationshipType.Contains,
                    Direction: RelationshipDirection.Outbound,
                    ObservationKind: ObservationKind.Observed,
                    Confidence: RelationshipConfidence.High,
                    Freshness: RelationshipFreshness.Live,
                    Status: ResourceHealthStatus.Healthy,
                    Evidence: [keyEvidence]));
            }
        }
        else if (focal.ResourceType == ResourceType.KvKey)
        {
            var parts = focal.ResourceId.Split('/', 2);
            if (parts.Length == 2)
            {
                var bucketName = parts[0];
                var keyNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.KvKey, focal.ResourceId);
                var bucketNodeId = ResourceNode.BuildNodeId(focal.EnvironmentId, ResourceType.KvBucket, bucketName);
                var evidence = new RelationshipEvidence(
                    SourceModule: RelationshipSourceModule.KeyValue,
                    EvidenceType: "KvKeyInBucket",
                    ObservedAt: DateTimeOffset.UtcNow,
                    Freshness: RelationshipFreshness.Live,
                    Summary: $"Key {parts[1]} belongs to bucket {bucketName}",
                    SafeFields: new Dictionary<string, string> { [BucketKey] = bucketName, ["key"] = parts[1] });

                edges.Add(new RelationshipEdge(
                    EdgeId: RelationshipEdge.BuildEdgeId(keyNodeId, bucketNodeId, RelationshipType.Contains, RelationshipSourceModule.KeyValue),
                    EnvironmentId: focal.EnvironmentId,
                    SourceNodeId: keyNodeId,
                    TargetNodeId: bucketNodeId,
                    RelationshipType: RelationshipType.Contains,
                    Direction: RelationshipDirection.Inbound,
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
        var buckets = await kvStoreAdapter.ListBucketsAsync(environmentId, ct);
        var bucketMap = buckets.ToDictionary(b => b.BucketName);

        foreach (var nodeId in nodeIds)
        {
            var parts = nodeId.Split(':', 3);
            if (parts.Length != 3) continue;

            if (parts[1] == "kvbucket" && bucketMap.TryGetValue(parts[2], out var bucket))
            {
                nodes.Add(new ResourceNode(
                    NodeId: nodeId,
                    EnvironmentId: environmentId,
                    ResourceType: ResourceType.KvBucket,
                    ResourceId: parts[2],
                    DisplayName: bucket.BucketName,
                    Status: ResourceHealthStatus.Healthy,
                    Freshness: RelationshipFreshness.Live,
                    IsFocal: false,
                    DetailRoute: null,
                    Metadata: new Dictionary<string, string>()));
            }
            else if (parts[1] == "kvkey")
            {
                var kvKeyNode = await TryResolveKvKeyNodeAsync(nodeId, parts[2], bucketMap, environmentId, ct);
                if (kvKeyNode is not null)
                    nodes.Add(kvKeyNode);
            }
        }

        return nodes;
    }

    private async Task<ResourceNode?> TryResolveKvKeyNodeAsync<TBucketInfo>(
        string nodeId, string keyPath, Dictionary<string, TBucketInfo> bucketMap, Guid environmentId, CancellationToken ct)
    {
        var keyParts = keyPath.Split('/', 2);
        if (keyParts.Length != 2 || !bucketMap.ContainsKey(keyParts[0]))
            return null;

        var key = await kvStoreAdapter.GetKeyAsync(environmentId, keyParts[0], keyParts[1], ct);
        if (key is null)
            return null;

        var status = key.Operation.Equals("DEL", StringComparison.OrdinalIgnoreCase)
            ? ResourceHealthStatus.Stale
            : ResourceHealthStatus.Healthy;

        return new ResourceNode(
            NodeId: nodeId,
            EnvironmentId: environmentId,
            ResourceType: ResourceType.KvKey,
            ResourceId: keyPath,
            DisplayName: key.Key,
            Status: status,
            Freshness: RelationshipFreshness.Live,
            IsFocal: false,
            DetailRoute: $"/kv/buckets/{Uri.EscapeDataString(keyParts[0])}/keys/{Uri.EscapeDataString(keyParts[1])}",
            Metadata: new Dictionary<string, string>
            {
                [BucketKey] = keyParts[0],
                ["revision"] = key.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["operation"] = key.Operation,
            });
    }
}

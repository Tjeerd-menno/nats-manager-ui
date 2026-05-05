using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Infrastructure.Relationships.Sources;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class KeyValueRelationshipSourceTests
{
    [Fact]
    public async Task ResolveNodesAsync_WhenKvKeyExists_ShouldReturnKeyNode()
    {
        var environmentId = Guid.NewGuid();
        var adapter = new FakeKvStoreAdapter
        {
            Buckets = [new KvBucketInfo("qa-bucket", 1, -1, -1, null, 1, 5)],
            KeysByBucket = new Dictionary<string, IReadOnlyList<KvEntry>>
            {
                ["qa-bucket"] = [new KvEntry("hello", "world", 1, "PUT", DateTimeOffset.UtcNow, 5)],
            },
        };
        var source = new KeyValueRelationshipSource(adapter);
        var nodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.KvKey, "qa-bucket/hello");

        var nodes = await source.ResolveNodesAsync([nodeId], environmentId, CancellationToken.None);

        var node = nodes.ShouldHaveSingleItem();
        node.NodeId.ShouldBe(nodeId);
        node.ResourceType.ShouldBe(ResourceType.KvKey);
        node.ResourceId.ShouldBe("qa-bucket/hello");
        node.DisplayName.ShouldBe("hello");
    }

    private sealed class FakeKvStoreAdapter : IKvStoreAdapter
    {
        public IReadOnlyList<KvBucketInfo> Buckets { get; init; } = [];
        public IReadOnlyDictionary<string, IReadOnlyList<KvEntry>> KeysByBucket { get; init; } =
            new Dictionary<string, IReadOnlyList<KvEntry>>();

        public Task<IReadOnlyList<KvBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Buckets);

        public Task<KvBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Buckets.SingleOrDefault(bucket => bucket.BucketName == bucketName));

        public Task CreateBucketAsync(Guid environmentId, string bucketName, int history, long maxBytes, int maxValueSize, TimeSpan? ttl, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<KvEntry>> ListKeysAsync(Guid environmentId, string bucketName, string? search, CancellationToken cancellationToken = default) =>
            Task.FromResult(KeysByBucket.TryGetValue(bucketName, out var keys) ? keys : []);

        public Task<KvEntry?> GetKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(KeysByBucket.TryGetValue(bucketName, out var keys)
                ? keys.SingleOrDefault(entry => entry.Key == key)
                : null);

        public Task<IReadOnlyList<KvKeyHistoryEntry>> GetKeyHistoryAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KvKeyHistoryEntry>>([]);

        public Task<long> PutKeyAsync(Guid environmentId, string bucketName, string key, byte[] value, long? expectedRevision, CancellationToken cancellationToken = default) =>
            Task.FromResult(1L);

        public Task DeleteKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

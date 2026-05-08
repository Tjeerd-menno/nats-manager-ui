using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Infrastructure.Relationships.Sources;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class ObjectStoreRelationshipSourceTests
{
    [Fact]
    public async Task ResolveNodesAsync_WhenObjectExists_ShouldReturnObjectNode()
    {
        var environmentId = Guid.NewGuid();
        var adapter = new FakeObjectStoreAdapter
        {
            Buckets = [new ObjectBucketInfo("qa-objects", 1, 18, "QA bucket")],
            ObjectsByBucket = new Dictionary<string, IReadOnlyList<ObjectInfo>>
            {
                ["qa-objects"] = [new ObjectInfo("qa-object.txt", 18, null, "text/plain", DateTimeOffset.UtcNow, 1, "digest")],
            },
        };
        var source = new ObjectStoreRelationshipSource(adapter);
        var nodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.ObjectStoreObject, "qa-objects/qa-object.txt");

        var nodes = await source.ResolveNodesAsync([nodeId], environmentId, CancellationToken.None);

        var node = nodes.ShouldHaveSingleItem();
        node.NodeId.ShouldBe(nodeId);
        node.ResourceType.ShouldBe(ResourceType.ObjectStoreObject);
        node.ResourceId.ShouldBe("qa-objects/qa-object.txt");
        node.DisplayName.ShouldBe("qa-object.txt");
    }

    private sealed class FakeObjectStoreAdapter : IObjectStoreAdapter
    {
        public IReadOnlyList<ObjectBucketInfo> Buckets { get; init; } = [];
        public IReadOnlyDictionary<string, IReadOnlyList<ObjectInfo>> ObjectsByBucket { get; init; } =
            new Dictionary<string, IReadOnlyList<ObjectInfo>>();

        public Task<IReadOnlyList<ObjectBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Buckets);

        public Task<ObjectBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Buckets.SingleOrDefault(bucket => bucket.BucketName == bucketName));

        public Task CreateBucketAsync(Guid environmentId, string bucketName, string? description, long? maxBucketSize, long? maxChunkSize, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<ObjectInfo>> ListObjectsAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default) =>
            Task.FromResult(ObjectsByBucket.TryGetValue(bucketName, out var objects) ? objects : []);

        public Task<ObjectInfo?> GetObjectInfoAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default) =>
            Task.FromResult(ObjectsByBucket.TryGetValue(bucketName, out var objects)
                ? objects.SingleOrDefault(obj => obj.Name == objectName)
                : null);

        public Task<byte[]> DownloadObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());

        public Task UploadObjectAsync(Guid environmentId, string bucketName, string objectName, byte[] data, string? contentType, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class ObjectStoreAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private ObjectStoreAdapter CreateAdapter() => new(ConnectionFactory, NullLogger<ObjectStoreAdapter>());

    [Fact]
    public async Task CreateBucket_ThenListBuckets_ShouldContainBucket()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];

        await adapter.CreateBucketAsync(EnvironmentId, bucketName, "test bucket", null, null);

        var buckets = await adapter.ListBucketsAsync(EnvironmentId);

        buckets.Should().Contain(b => b.BucketName == bucketName);
    }

    [Fact]
    public async Task GetBucketAsync_WithExistingBucket_ShouldReturnInfo()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, "desc", null, null);

        var info = await adapter.GetBucketAsync(EnvironmentId, bucketName);

        info.Should().NotBeNull();
        info!.BucketName.Should().Be(bucketName);
        info.Description.Should().Be("desc");
    }

    [Fact]
    public async Task GetBucketAsync_WithNonExistentBucket_ShouldReturnNull()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetBucketAsync(EnvironmentId, "nonexistent-bucket");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UploadObject_ThenDownload_ShouldReturnSameData()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, null, null, null);

        var data = "Hello, Object Store!"u8.ToArray();
        await adapter.UploadObjectAsync(EnvironmentId, bucketName, "greeting.txt", data, "text/plain");

        var downloaded = await adapter.DownloadObjectAsync(EnvironmentId, bucketName, "greeting.txt");

        downloaded.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task ListObjectsAsync_ShouldReturnUploadedObjects()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, null, null, null);
        await adapter.UploadObjectAsync(EnvironmentId, bucketName, "file1.txt", "one"u8.ToArray(), null);
        await adapter.UploadObjectAsync(EnvironmentId, bucketName, "file2.txt", "two"u8.ToArray(), null);

        var objects = await adapter.ListObjectsAsync(EnvironmentId, bucketName);

        objects.Should().HaveCount(2);
        objects.Select(o => o.Name).Should().Contain(["file1.txt", "file2.txt"]);
    }

    [Fact]
    public async Task GetObjectInfoAsync_ShouldReturnMetadata()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, null, null, null);
        await adapter.UploadObjectAsync(EnvironmentId, bucketName, "info-test.bin", new byte[] { 1, 2, 3, 4 }, null);

        var info = await adapter.GetObjectInfoAsync(EnvironmentId, bucketName, "info-test.bin");

        info.Should().NotBeNull();
        info!.Name.Should().Be("info-test.bin");
        info.Size.Should().Be(4);
    }

    [Fact]
    public async Task DeleteObjectAsync_ShouldRemoveObject()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, null, null, null);
        await adapter.UploadObjectAsync(EnvironmentId, bucketName, "to-delete.txt", "bye"u8.ToArray(), null);

        await adapter.DeleteObjectAsync(EnvironmentId, bucketName, "to-delete.txt");

        var info = await adapter.GetObjectInfoAsync(EnvironmentId, bucketName, "to-delete.txt");
        info.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBucketAsync_ShouldRemoveBucket()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test{Guid.NewGuid():N}"[..16];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, null, null, null);

        await adapter.DeleteBucketAsync(EnvironmentId, bucketName);

        var info = await adapter.GetBucketAsync(EnvironmentId, bucketName);
        info.Should().BeNull();
    }
}

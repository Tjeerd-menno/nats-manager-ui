using System.Text;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class KvStoreAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private KvStoreAdapter CreateAdapter() => new(ConnectionFactory, NullLogger<KvStoreAdapter>());

    [Fact]
    public async Task CreateBucket_ThenGetBucket_ShouldReturnBucket()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test_{Guid.NewGuid():N}"[..20];

        await adapter.CreateBucketAsync(EnvironmentId, bucketName, history: 5, maxBytes: -1, maxValueSize: -1, ttl: null);

        var bucket = await adapter.GetBucketAsync(EnvironmentId, bucketName);

        bucket.Should().NotBeNull();
        bucket!.BucketName.Should().Be(bucketName);
    }

    [Fact]
    public async Task GetBucketAsync_WithExistingBucket_ShouldReturnInfo()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);

        var info = await adapter.GetBucketAsync(EnvironmentId, bucketName);

        info.Should().NotBeNull();
        info!.BucketName.Should().Be(bucketName);
        info.History.Should().Be(5);
    }

    [Fact]
    public async Task GetBucketAsync_WithNonExistentBucket_ShouldReturnNull()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetBucketAsync(EnvironmentId, "nonexistent-bucket");

        result.Should().BeNull();
    }

    [Fact]
    public async Task PutKey_ThenGetKey_ShouldReturnStoredValue()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);

        var value = Encoding.UTF8.GetBytes("test-value");
        var revision = await adapter.PutKeyAsync(EnvironmentId, bucketName, "my-key", value, null);

        revision.Should().BeGreaterThan(0);

        var entry = await adapter.GetKeyAsync(EnvironmentId, bucketName, "my-key");

        entry.Should().NotBeNull();
        entry!.Key.Should().Be("my-key");
        entry.Revision.Should().Be(revision);
    }

    [Fact]
    public async Task GetKeyAsync_WithNonExistentKey_ShouldReturnNull()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);

        var result = await adapter.GetKeyAsync(EnvironmentId, bucketName, "nonexistent-key");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ListKeysAsync_ShouldReturnStoredKeys()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "key-a", "a"u8.ToArray(), null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "key-b", "b"u8.ToArray(), null);

        var keys = await adapter.ListKeysAsync(EnvironmentId, bucketName, null);

        keys.Should().HaveCount(2);
        keys.Select(k => k.Key).Should().Contain(["key-a", "key-b"]);
    }

    [Fact]
    public async Task ListKeysAsync_WithSearch_ShouldFilterKeys()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "user.name", "alice"u8.ToArray(), null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "config.db", "sqlite"u8.ToArray(), null);

        var keys = await adapter.ListKeysAsync(EnvironmentId, bucketName, "user");

        keys.Should().ContainSingle(k => k.Key == "user.name");
    }

    [Fact]
    public async Task DeleteKeyAsync_ShouldRemoveKey()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "to-delete", "val"u8.ToArray(), null);

        await adapter.DeleteKeyAsync(EnvironmentId, bucketName, "to-delete");

        var entry = await adapter.GetKeyAsync(EnvironmentId, bucketName, "to-delete");
        entry.Should().BeNull();
    }

    [Fact]
    public async Task GetKeyHistoryAsync_ShouldReturnRevisions()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 10, -1, -1, null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "versioned", "v1"u8.ToArray(), null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "versioned", "v2"u8.ToArray(), null);
        await adapter.PutKeyAsync(EnvironmentId, bucketName, "versioned", "v3"u8.ToArray(), null);

        var history = await adapter.GetKeyHistoryAsync(EnvironmentId, bucketName, "versioned");

        history.Should().HaveCount(3);
        history.Select(h => h.Revision).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task DeleteBucketAsync_ShouldRemoveBucket()
    {
        var adapter = CreateAdapter();
        var bucketName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateBucketAsync(EnvironmentId, bucketName, 5, -1, -1, null);

        await adapter.DeleteBucketAsync(EnvironmentId, bucketName);

        var info = await adapter.GetBucketAsync(EnvironmentId, bucketName);
        info.Should().BeNull();
    }
}

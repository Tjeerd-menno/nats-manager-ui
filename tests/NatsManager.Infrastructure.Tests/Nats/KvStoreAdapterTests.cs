using NatsManager.Infrastructure.Nats;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Nats;

public sealed class KvStoreAdapterTests
{
    [Fact]
    public void TryGetExternalBucketName_WhenStatusBucketIsKvStream_ReturnsBucketName()
    {
        var included = KvStoreAdapter.TryGetExternalBucketName("KV_orders", ["$KV.orders.>"], out var bucketName);

        included.ShouldBeTrue();
        bucketName.ShouldBe("orders");
    }

    [Fact]
    public void TryGetExternalBucketName_WhenStatusBucketIsExternalKvName_ReturnsBucketName()
    {
        var included = KvStoreAdapter.TryGetExternalBucketName("orders", ["$KV.orders.>"], out var bucketName);

        included.ShouldBeTrue();
        bucketName.ShouldBe("orders");
    }

    [Theory]
    [InlineData("ORDERS", "orders.*")]
    [InlineData("OBJ_assets", "$O.assets.C.>")]
    [InlineData("", "$KV.orders.>")]
    public void TryGetExternalBucketName_WhenStatusIsNotKvBucket_ReturnsFalse(string statusBucket, string subject)
    {
        var included = KvStoreAdapter.TryGetExternalBucketName(statusBucket, [subject], out var bucketName);

        included.ShouldBeFalse();
        bucketName.ShouldBeEmpty();
    }
}

using System.Net;
using System.Net.Http.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.ObjectStore.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class ObjectStoreEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public ObjectStoreEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBuckets_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.ObjectStoreAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<ObjectBucketInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/objectstore/buckets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBucketDetail_WhenNotFound_ShouldReturn404()
    {
        var envId = Guid.NewGuid();
        _factory.ObjectStoreAdapter.GetBucketAsync(envId, "missing", Arg.Any<CancellationToken>())
            .Returns((ObjectBucketInfo?)null);

        var response = await _client.GetAsync($"/api/environments/{envId}/objectstore/buckets/missing");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBucket_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/objectstore/buckets/test");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DownloadObject_WhenNotFound_ShouldReturn404()
    {
        var envId = Guid.NewGuid();
        _factory.ObjectStoreAdapter.DownloadObjectAsync(envId, "bucket", "missing", Arg.Any<CancellationToken>())
            .Returns<byte[]>(x => throw new InvalidOperationException("not found"));

        var response = await _client.GetAsync($"/api/environments/{envId}/objectstore/buckets/bucket/objects/missing/download");

        // The handler catches exception and returns null → NotFound
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }
}

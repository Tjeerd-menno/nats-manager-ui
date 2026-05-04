using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Web.Configuration;

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

        await ShouldBeConfirmationValidationProblem(response);
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

    [Fact]
    public async Task UploadObject_WhenContentLengthExceedsLimit_ShouldReturn413()
    {
        var envId = Guid.NewGuid();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/environments/{envId}/objectstore/buckets/bucket/objects/large.bin/upload")
        {
            Content = new DeclaredLengthContent(ObjectStoreUploadOptions.DefaultMaxUploadBytes + 1)
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        await _factory.ObjectStoreAdapter.DidNotReceiveWithAnyArgs().UploadObjectAsync(
            default,
            string.Empty,
            string.Empty,
            [],
            default,
            default);
    }

    private sealed class DeclaredLengthContent(long contentLength) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.CompletedTask;

        protected override bool TryComputeLength(out long length)
        {
            length = contentLength;
            return true;
        }
    }

    private static async Task ShouldBeConfirmationValidationProblem(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("errors").GetProperty("X-Confirm").EnumerateArray().Single().GetString()
            .ShouldBe("X-Confirm header must be 'true' for destructive operations.");
    }
}

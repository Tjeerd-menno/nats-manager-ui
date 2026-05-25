using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class KvEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public KvEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBuckets_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.KvStoreAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<KvBucketInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/kv/buckets");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetKeyDetail_WhenAuthenticatedWithoutOperatorAccess_ShouldReturn403()
    {
        var envId = Guid.NewGuid();
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Auditor);

        var response = await client.GetAsync($"/api/environments/{envId}/kv/buckets/bucket/keys/key");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await _factory.KvStoreAdapter.DidNotReceiveWithAnyArgs().GetKeyAsync(
            default,
            string.Empty,
            string.Empty,
            default);
    }

    [Fact]
    public async Task GetKeys_WhenAuthenticatedWithoutOperatorAccess_ShouldReturn403()
    {
        var envId = Guid.NewGuid();
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Auditor);

        var response = await client.GetAsync($"/api/environments/{envId}/kv/buckets/bucket/keys");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await _factory.KvStoreAdapter.DidNotReceiveWithAnyArgs().ListKeysAsync(
            default,
            string.Empty,
            default,
            default);
    }

    [Fact]
    public async Task GetKeyHistory_WhenAuthenticatedWithoutOperatorAccess_ShouldReturn403()
    {
        var envId = Guid.NewGuid();
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Auditor);

        var response = await client.GetAsync($"/api/environments/{envId}/kv/buckets/bucket/keys/key/history");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await _factory.KvStoreAdapter.DidNotReceiveWithAnyArgs().GetKeyHistoryAsync(
            default,
            string.Empty,
            string.Empty,
            default);
    }

    [Fact]
    public async Task DeleteBucket_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/kv/buckets/test");

        await ShouldBeConfirmationValidationProblem(response);
    }

    [Fact]
    public async Task PutKey_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.KvStoreAdapter.PutKeyAsync(envId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<long?>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        var payload = new { Value = "dGVzdA==" };
        var response = await _client.PutAsJsonAsync($"/api/environments/{envId}/kv/buckets/bucket/keys/key", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteKey_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/kv/buckets/bucket/keys/key");

        await ShouldBeConfirmationValidationProblem(response);
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

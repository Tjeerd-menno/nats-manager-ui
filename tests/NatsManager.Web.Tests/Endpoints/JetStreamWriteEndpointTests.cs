using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class JetStreamWriteEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;

    public JetStreamWriteEndpointTests(NatsManagerWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteStream_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/jetstream/streams/orders");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PurgeStream_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/environments/{envId}/jetstream/streams/orders/purge", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteConsumer_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/jetstream/streams/orders/consumers/worker");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateStream_WithValidPayload_ShouldReturn201()
    {
        var envId = Guid.NewGuid();
        var payload = new { Name = "test-stream", Subjects = new[] { "test.>" }, RetentionPolicy = "Limits", StorageType = "File" };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/jetstream/streams", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

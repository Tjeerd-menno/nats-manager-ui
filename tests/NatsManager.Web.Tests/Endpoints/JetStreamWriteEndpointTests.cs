using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Domain.Modules.Auth;
using NatsEnvironment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class JetStreamWriteEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public JetStreamWriteEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeleteStream_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/jetstream/streams/orders");

        await ShouldBeConfirmationValidationProblem(response);
    }

    [Fact]
    public async Task PurgeStream_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/environments/{envId}/jetstream/streams/orders/purge", null);

        await ShouldBeConfirmationValidationProblem(response);
    }

    [Fact]
    public async Task DeleteConsumer_WithoutConfirmHeader_ShouldReturn400()
    {
        var envId = Guid.NewGuid();
        var response = await _client.DeleteAsync($"/api/environments/{envId}/jetstream/streams/orders/consumers/worker");

        await ShouldBeConfirmationValidationProblem(response);
    }

    [Fact]
    public async Task CreateStream_WithValidPayload_ShouldReturn201()
    {
        var envId = Guid.NewGuid();
        var payload = new { Name = "test-stream", Subjects = new[] { "test.>" }, RetentionPolicy = "Limits", StorageType = "File" };

        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/jetstream/streams", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateStream_InProductionAsOperator_ShouldReturn403()
    {
        var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Operator);
        var env = NatsEnvironment.Create("prod", "nats://localhost:4222", isProduction: true);
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>())
            .Returns(env);
        var payload = new { Name = "test-stream", Subjects = new[] { "test.>" }, RetentionPolicy = "Limits", StorageType = "File" };

        var response = await client.PostAsJsonAsync($"/api/environments/{env.Id}/jetstream/streams", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
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

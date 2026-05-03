using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.Environments.Commands;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Domain.Modules.Common;
using NatsManager.Web.Endpoints;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class EnvironmentEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public EnvironmentEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEnvironments_ShouldReturn200()
    {
        var envs = new List<Environment> { Environment.Create("Test", "nats://localhost:4222") };
        _factory.EnvironmentRepository.GetPagedAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns((envs.AsReadOnly() as IReadOnlyList<Environment>, envs.Count));

        var response = await _client.GetAsync("/api/environments");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEnvironmentDetail_ShouldReturn200()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(env);

        var response = await _client.GetAsync($"/api/environments/{env.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestConnection_ShouldReturn200()
    {
        var env = Environment.Create("Test", "nats://localhost:4222");
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(env);
        _factory.HealthChecker.CheckHealthAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>())
            .Returns(new TestConnectionResult(true, 5, "2.10.0", true));

        var response = await _client.PostAsync($"/api/environments/{env.Id}/test", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestConnection_WithScopedOperatorForMatchingEnvironment_ShouldReturn200()
    {
        var env = Environment.Create("Scoped", "nats://localhost:4222");
        using var client = _factory.CreateAuthenticatedClientWithScopedRole(Role.PredefinedNames.Operator, env.Id);
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(env);
        _factory.HealthChecker.CheckHealthAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>())
            .Returns(new TestConnectionResult(true, 5, "2.10.0", true));

        var response = await client.PostAsync($"/api/environments/{env.Id}/test", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TestConnection_WithScopedOperatorForDifferentEnvironment_ShouldReturn403()
    {
        var assignedEnvironmentId = Guid.NewGuid();
        var requestedEnvironmentId = Guid.NewGuid();
        using var client = _factory.CreateAuthenticatedClientWithScopedRole(Role.PredefinedNames.Operator, assignedEnvironmentId);

        var response = await client.PostAsync($"/api/environments/{requestedEnvironmentId}/test", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterEnvironment_WithInvalidServerUrl_ShouldReturn400ValidationProblem()
    {
        _factory.EnvironmentRepository.ClearReceivedCalls();
        var request = new RegisterEnvironmentRequest(
            "Invalid",
            null,
            "tcp://localhost:4222",
            CredentialType.None,
            null,
            false);

        var response = await _client.PostAsJsonAsync("/api/environments", request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("title").GetString().ShouldBe("One or more validation errors occurred.");
        var serverUrlError = json.RootElement.GetProperty("errors")
            .GetProperty(nameof(RegisterEnvironmentCommand.ServerUrl))
            .EnumerateArray()
            .Single()
            .GetString();
        serverUrlError.ShouldBe("ServerUrl must be an absolute URL using one of the allowed schemes: nats://, tls://, ws://, wss://.");
        await _factory.EnvironmentRepository.DidNotReceive().AddAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEnvironment_WhenOperatorChangesMonitoringUrl_ShouldReturn403()
    {
        var client = _factory.CreateAuthenticatedClient(NatsManager.Domain.Modules.Auth.Role.PredefinedNames.Operator);
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.ClearReceivedCalls();
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(env);
        var request = new UpdateEnvironmentRequest(
            "Test",
            null,
            "nats://localhost:4222",
            CredentialType.None,
            null,
            false,
            true,
            "http://localhost:8223",
            30);

        var response = await client.PutAsJsonAsync($"/api/environments/{env.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await _factory.EnvironmentRepository.DidNotReceive().UpdateAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEnvironment_WhenOperatorKeepsMonitoringUrl_ShouldReturn200()
    {
        var client = _factory.CreateAuthenticatedClient(NatsManager.Domain.Modules.Auth.Role.PredefinedNames.Operator);
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateMonitoringSettings("http://localhost:8222", 30);
        _factory.EnvironmentRepository.ClearReceivedCalls();
        _factory.EnvironmentRepository.GetByIdAsync(env.Id, Arg.Any<CancellationToken>()).Returns(env);
        _factory.EnvironmentRepository.ExistsWithNameAsync("Test", env.Id, Arg.Any<CancellationToken>()).Returns(false);
        var request = new UpdateEnvironmentRequest(
            "Test",
            null,
            "nats://localhost:4222",
            CredentialType.None,
            null,
            false,
            true,
            " http://localhost:8222 ",
            30);

        var response = await client.PutAsJsonAsync($"/api/environments/{env.Id}", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

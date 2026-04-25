using System.Net;
using System.Net.Http.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.Environments.Commands;
using NatsManager.Domain.Modules.Common;
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
}

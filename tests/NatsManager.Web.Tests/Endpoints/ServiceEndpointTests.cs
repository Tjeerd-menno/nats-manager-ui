using System.Net;
using System.Net.Http.Json;
using Shouldly;
using NSubstitute;
using NatsManager.Application.Modules.Services.Models;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class ServiceEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public ServiceEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetServices_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.ServiceDiscoveryAdapter.DiscoverServicesAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new List<ServiceInfo>());

        var response = await _client.GetAsync($"/api/environments/{envId}/services");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetServiceDetail_WhenNotFound_ShouldReturn404()
    {
        var envId = Guid.NewGuid();
        _factory.ServiceDiscoveryAdapter.GetServiceAsync(envId, "missing", Arg.Any<CancellationToken>())
            .Returns((ServiceInfo?)null);

        var response = await _client.GetAsync($"/api/environments/{envId}/services/missing");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TestServiceRequest_ShouldReturn200()
    {
        var envId = Guid.NewGuid();
        _factory.ServiceDiscoveryAdapter.TestServiceRequestAsync(envId, Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("response");

        var payload = new { Subject = "test.subject" };
        var response = await _client.PostAsJsonAsync($"/api/environments/{envId}/services/svc/test", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

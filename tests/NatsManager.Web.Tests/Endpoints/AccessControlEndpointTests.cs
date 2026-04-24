using System.Net;
using Shouldly;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class AccessControlEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly NatsManagerWebAppFactory _factory;

    public AccessControlEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRoles_WhenAnonymous_ShouldReturn401()
    {
        using var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/access-control/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRoles_WhenNonAdmin_ShouldReturn403()
    {
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Auditor);

        var response = await client.GetAsync("/api/access-control/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoles_WhenAdmin_ShouldReturn200()
    {
        using var client = _factory.CreateAuthenticatedClient(Role.PredefinedNames.Administrator);

        var response = await client.GetAsync("/api/access-control/roles");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

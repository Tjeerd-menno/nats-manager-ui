using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class AuthEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly NatsManagerWebAppFactory _factory;

    public AuthEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        var payload = new { Username = "unknown", Password = "wrong" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldReturn200()
    {
        using var authenticatedClient = _factory.CreateAuthenticatedClient();
        var response = await authenticatedClient.PostAsync("/api/auth/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

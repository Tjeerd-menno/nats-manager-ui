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
    public async Task GetCurrentUser_WhenNotAuthenticated_ShouldReturn200WithNullBody()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldBe("null");
    }

    [Fact]
    public async Task GetAuthConfig_WhenOidcIsNotConfigured_ShouldReturnDisabledOidc()
    {
        var config = await _client.GetFromJsonAsync<AuthConfigResponse>("/api/auth/config");

        config.ShouldNotBeNull();
        config.OidcEnabled.ShouldBeFalse();
        config.OidcLoginPath.ShouldBe("/api/auth/oidc/login");
    }

    [Fact]
    public async Task Logout_ShouldReturn200()
    {
        using var authenticatedClient = _factory.CreateAuthenticatedClient();
        var response = await authenticatedClient.PostAsync("/api/auth/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OidcLogin_WhenOidcIsNotConfigured_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/auth/oidc/login?returnUrl=%2Fdashboard");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("https://evil.example/path", "/")]
    [InlineData("//evil.com", "/")]
    [InlineData("\\\\evil.com", "/")]
    [InlineData("/\\\\evil.com", "/")]
    public async Task OidcLogout_WhenReturnUrlIsProvided_ShouldUseSafeRedirect(string returnUrl, string expectedLocation)
    {
        using var authenticatedClient = CreateAuthenticatedClientWithoutRedirect();

        var response = await authenticatedClient.PostAsync($"/api/auth/oidc/logout?returnUrl={Uri.EscapeDataString(returnUrl)}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldBe(expectedLocation);
    }

    private HttpClient CreateAuthenticatedClientWithoutRedirect()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "authenticated");
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, TestAuthHandler.DefaultUserId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.UsernameHeaderName, NatsManagerWebAppFactory.BootstrapAdminUsername);
        client.DefaultRequestHeaders.Add(TestAuthHandler.DisplayNameHeaderName, "Administrator");
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeaderName, "Administrator");
        return client;
    }

    private sealed record AuthConfigResponse(bool OidcEnabled, string OidcLoginPath);
}

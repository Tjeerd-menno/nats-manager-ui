using System.Net;
using Shouldly;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class SearchEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client;

    public SearchEndpointTests(NatsManagerWebAppFactory factory)
    {
        _client = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task GetBookmarks_WhenNotAuthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/bookmarks");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferences_WhenNotAuthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/preferences");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}

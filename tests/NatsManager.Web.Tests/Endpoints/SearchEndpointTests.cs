using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using NatsManager.Domain.Modules.Common;
using NatsManager.Infrastructure.Persistence;
using NatsManager.Web.Endpoints;
using NatsEnvironment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class SearchEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly NatsManagerWebAppFactory _factory;

    public SearchEndpointTests(NatsManagerWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBookmarks_WhenNotAuthenticated_ShouldReturn401()
    {
        using var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/bookmarks");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPreferences_WhenNotAuthenticated_ShouldReturn401()
    {
        using var client = _factory.CreateAnonymousClient();

        var response = await client.GetAsync("/api/preferences");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_WhenAuthenticated_ShouldReturnMatchingBookmarks()
    {
        var uniqueName = $"Orders {Guid.NewGuid():N}";
        var environment = NatsEnvironment.Create($"Search {Guid.NewGuid():N}", "nats://localhost:4222");
        Guid userId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.Environments.Add(environment);
            await context.SaveChangesAsync();
            userId = context.Users.Single(user => user.Username == NatsManagerWebAppFactory.BootstrapAdminUsername).Id;
        }

        using var client = _factory.CreateAuthenticatedClient(userId);
        var request = new AddBookmarkRequest(environment.Id, ResourceType.Stream, "orders-stream", uniqueName);
        var addResponse = await client.PostAsJsonAsync("/api/bookmarks", request);
        addResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await client.GetAsync($"/api/search?q={Uri.EscapeDataString(uniqueName)}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var result = json.RootElement.EnumerateArray().ShouldHaveSingleItem();
        result.GetProperty("displayName").GetString().ShouldBe(uniqueName);
        result.GetProperty("resourceId").GetString().ShouldBe("orders-stream");
        result.GetProperty("resourceType").GetString().ShouldBe(nameof(ResourceType.Stream));
    }
}

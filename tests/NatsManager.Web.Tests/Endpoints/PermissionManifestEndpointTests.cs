using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Services;
using NatsManager.Web.Configuration;
using Shouldly;

namespace NatsManager.Web.Tests.Endpoints;

public sealed class PermissionManifestEndpointTests : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly NatsManagerWebAppFactory factory;

    public PermissionManifestEndpointTests(NatsManagerWebAppFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetPermissions_InPublicMode_ShouldReturnManifest()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        response.Headers.GetValues("X-Permission-Manifest-Source").Single().ShouldBe("current");

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        root.GetProperty("application").GetProperty("id").GetString().ShouldBe("nats-manager");
        root.GetProperty("application").GetProperty("name").GetString().ShouldBe("NATS Manager");
        root.GetProperty("permissions").GetArrayLength().ShouldBeGreaterThan(0);
        root.GetProperty("permissions")[0].GetProperty("name").GetString().ShouldNotBeNullOrWhiteSpace();
        root.GetProperty("permissions")[0].GetProperty("description").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetPermissions_WithVersionAndCategories_ShouldReturnOptionalMetadata()
    {
        await using var app = factory.WithPermissionManifestConfiguration(new Dictionary<string, string?>
        {
            ["PermissionManifest:ApplicationVersion"] = "1.2.3"
        });
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("application").GetProperty("version").GetString().ShouldBe("1.2.3");
        var hasCategory = document.RootElement.GetProperty("permissions")
            .EnumerateArray()
            .Any(permission => permission.TryGetProperty("category", out var category)
                && !string.IsNullOrWhiteSpace(category.GetString()));
        hasCategory.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPermissions_WhenLastValidManifestIsUsed_ShouldReturnLastValidSourceHeader()
    {
        var manifest = TestManifest("read:environments");
        await using var app = factory.WithPublisher(new StubPermissionManifestPublisher(
            ManifestRetrievalResult.LastValid(manifest, "Current manifest is invalid.")));
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Permission-Manifest-Source").Single().ShouldBe("last-valid");
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("permissions")[0].GetProperty("name").GetString().ShouldBe("read:environments");
    }

    [Fact]
    public async Task GetPermissions_WhenNoValidManifestExists_ShouldReturn503Problem()
    {
        await using var app = factory.WithPublisher(new StubPermissionManifestPublisher(
            ManifestRetrievalResult.Failure("No valid permission manifest is currently available.")));
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Permission manifest unavailable");
        problem.Status.ShouldBe(503);
    }

    [Fact]
    public async Task GetPermissions_InRestrictedModeWithoutKey_ShouldReturn401()
    {
        await using var app = factory.WithRestrictedPermissionManifest("service-secret");
        using var client = app.CreateClient();

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissions_InRestrictedModeWithInvalidKey_ShouldReturn401()
    {
        await using var app = factory.WithRestrictedPermissionManifest("service-secret");
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Permission-Manifest-Key", "wrong-secret");

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPermissions_InRestrictedModeWithValidKey_ShouldReturnManifest()
    {
        await using var app = factory.WithRestrictedPermissionManifest("service-secret");
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Permission-Manifest-Key", "service-secret");

        using var response = await client.GetAsync("/.well-known/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Permission-Manifest-Source").Single().ShouldBe("current");
    }

    private static PermissionManifest TestManifest(string permissionName) =>
        new(
            new ApplicationMetadata("nats-manager", "NATS Manager"),
            [new PermissionDefinition(permissionName, "Allows reading environments", "Environments")]);

    private sealed class StubPermissionManifestPublisher(
        ManifestRetrievalResult retrievalResult) : IPermissionManifestPublisher
    {
        public PermissionManifestPublicationState State { get; private set; } =
            new(null, null, null, PermissionManifestPublicationStatus.InvalidNoFallback);

        public PermissionManifestPublicationResult Publish(PermissionManifest? manifest) =>
            new(PermissionManifestPublicationStatus.Valid, true, null);

        public ManifestRetrievalResult GetManifest() => retrievalResult;
    }
}

internal static class PermissionManifestEndpointTestFactoryExtensions
{
    public static WebApplicationFactory<Program> WithPermissionManifestConfiguration(
        this NatsManagerWebAppFactory factory,
        IDictionary<string, string?> configuration) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(configuration);
            });
        });

    public static WebApplicationFactory<Program> WithRestrictedPermissionManifest(
        this NatsManagerWebAppFactory factory,
        string accessKey) =>
        factory.WithPermissionManifestConfiguration(new Dictionary<string, string?>
        {
            ["PermissionManifest:ExposureMode"] = "Restricted",
            ["PermissionManifest:RestrictedAccessKeyHash"] = PermissionManifestOptions.HashAccessKey(accessKey)
        });

    public static WebApplicationFactory<Program> WithPublisher(
        this NatsManagerWebAppFactory factory,
        IPermissionManifestPublisher publisher) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPermissionManifestPublisher>();
                services.AddSingleton(publisher);
            });
        });
}

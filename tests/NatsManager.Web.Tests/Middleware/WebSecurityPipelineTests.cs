using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NatsManager.Web.Configuration;
using Shouldly;

namespace NatsManager.Web.Tests.Middleware;

/// <summary>
/// Covers the two request-pipeline protections that the endpoint test factory opts out of:
/// antiforgery validation on unsafe <c>/api</c> requests, and the login rate limiter.
/// These run against the real pipeline with the protection switched back on, so a
/// regression in <see cref="WebApplicationExtensions.UseNatsManagerPipeline"/> — or an
/// accidental default flip in <see cref="WebSecurityOptions"/> — fails here.
/// </summary>
public sealed class WebSecurityPipelineTests(NatsManagerWebAppFactory factory)
    : IClassFixture<NatsManagerWebAppFactory>
{
    [Fact]
    public void SecurityOptionDefaults_ShouldEnableEveryProtection()
    {
        // A deployment that configures nothing must run fully protected. These are opt-out
        // switches for tests, not opt-in features.
        var defaults = new WebSecurityOptions();

        defaults.EnableAntiforgery.ShouldBeTrue();
        defaults.EnableRateLimiting.ShouldBeTrue();
        defaults.EnableHttpsRedirection.ShouldBeTrue();
    }

    [Fact]
    public async Task UnsafeApiRequest_WithoutAntiforgeryToken_ShouldReturn400()
    {
        using var app = factory.WithSecurityOptions(antiforgery: true);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "authenticated");

        var response = await client.PostAsJsonAsync(
            "/api/environments",
            new { Name = "env", ServerUrl = "nats://localhost:4222", CredentialType = "None" },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("X-XSRF-TOKEN");
    }

    [Fact]
    public async Task ApiGet_WithAntiforgeryEnabled_ShouldIssueReadableXsrfCookie()
    {
        using var app = factory.WithSecurityOptions(antiforgery: true);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "anonymous");

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setCookies = response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values
            : Array.Empty<string>();
        var xsrfCookie = setCookies.FirstOrDefault(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));

        xsrfCookie.ShouldNotBeNull();
        // The SPA has to read this value back out to echo it in the header, so it must
        // not be HttpOnly.
        xsrfCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }

    [Fact]
    public async Task LoginEndpoint_AfterExceedingPermitLimit_ShouldReturn429()
    {
        using var app = factory.WithSecurityOptions(rateLimiting: true);
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.AuthModeHeaderName, "anonymous");
        var payload = new { Username = "unknown", Password = "wrong" };

        // The login policy permits 5 attempts per minute per client IP.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var allowed = await client.PostAsJsonAsync(
                "/api/auth/login", payload, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(
                HttpStatusCode.Unauthorized,
                $"attempt {attempt} should still be within the permit limit");
        }

        var throttled = await client.PostAsJsonAsync(
            "/api/auth/login", payload, TestContext.Current.CancellationToken);

        throttled.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }
}

internal static class WebSecurityTestFactoryExtensions
{
    /// <summary>
    /// Re-enables the pipeline protections that <see cref="NatsManagerWebAppFactory"/>
    /// switches off, so a test can exercise the shipping configuration. HTTPS redirection
    /// stays off — the test server speaks plain HTTP.
    /// </summary>
    public static WebApplicationFactory<Program> WithSecurityOptions(
        this NatsManagerWebAppFactory factory,
        bool antiforgery = false,
        bool rateLimiting = false) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{WebSecurityOptions.SectionName}:EnableAntiforgery"] =
                        antiforgery ? "true" : "false",
                    [$"{WebSecurityOptions.SectionName}:EnableRateLimiting"] =
                        rateLimiting ? "true" : "false"
                })));
}

using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Environment Selector component in the sidebar.
/// </summary>
public sealed class EnvironmentSelectorTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task EnvironmentSelector_ShowsSelectPlaceholder()
    {
        await LoginAsAdminAsync("/dashboard");

        var envSelector = Page.GetByPlaceholder("Select environment");
        await Expect(envSelector).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task EnvironmentSelector_ShowsRegisteredEnvironments()
    {
        var envName = $"e2e-sel-{Guid.NewGuid():N}"[..16];

        // Register an environment via API
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient)
        using (handler)
        {
            await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Inject session cookie
            var backendCookies = handler.CookieContainer.GetCookies(new Uri(Fixture.BackendUrl));
            var sessionCookie = backendCookies[".AspNetCore.Session"]!;
            var frontendUri = new Uri(Fixture.FrontendUrl);
            await Page.Context.AddCookiesAsync(
            [
                new()
                {
                    Name = sessionCookie.Name,
                    Value = sessionCookie.Value,
                    Domain = frontendUri.Host,
                    Path = sessionCookie.Path,
                    HttpOnly = sessionCookie.HttpOnly,
                    Secure = sessionCookie.Secure,
                    SameSite = SameSiteAttribute.Strict,
                }
            ]);
        }

        await NavigateAsync("/dashboard");

        // Click the environment selector
        var envSelector = Page.GetByPlaceholder("Select environment");
        await envSelector.ClickAsync();

        // The registered environment should appear in the dropdown
        await Expect(Page.GetByText(envName)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task EnvironmentSelector_SelectingEnvironment_EnablesNatsPages()
    {
        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // After selecting an environment, the JetStream page should show content
        // (not the "Select an environment" message)
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

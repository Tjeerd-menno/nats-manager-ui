using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

public sealed class LogoutTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task LogoutButton_ClearsSessionAndRedirectsToLogin()
    {
        await LoginAsAdminAsync("/dashboard");

        // Verify we're on the dashboard
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click the logout button (aria-label="Logout")
        await Page.GetByLabel("Logout").ClickAsync();

        // Should redirect to login page
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AfterLogout_NavigatingToDashboard_RedirectsToLogin()
    {
        await LoginAsAdminAsync("/dashboard");

        // Logout
        await Page.GetByLabel("Logout").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Try navigating to a protected page
        await NavigateAsync("/dashboard");

        // Should still be on login
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

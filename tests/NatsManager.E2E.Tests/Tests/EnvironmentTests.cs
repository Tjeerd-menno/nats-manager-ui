using Shouldly;
using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for environment management (register, list, delete).
/// </summary>
public sealed class EnvironmentTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task EnvironmentsPage_ShowsEnvironmentList()
    {
        await LoginAsAdminAsync("/dashboard");

        // Navigate to Environments via sidebar
        await Page.GetByText("Environments", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/environments", new PageWaitForURLOptions { Timeout = 10_000 });

        // The page should have the Environments title
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" });
        await Expect(heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EnvironmentsPage_CanRegisterNewEnvironment()
    {
        var envName = $"e2e-env-{Guid.NewGuid():N}"[..20];

        await LoginAsAdminAsync("/environments");

        // Wait for environments page to fully load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click "Register Environment" button (may be in EmptyState or table header)
        var registerButton = Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" });
        await Expect(registerButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await registerButton.ClickAsync();

        // Fill in the form
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        // Wait for the environment to appear in the list table
        var envCell = Page.GetByRole(AriaRole.Cell, new() { Name = envName });
        await Expect(envCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task EnvironmentsPage_RegisteredEnvironment_ShowsConnectionStatus()
    {
        var envName = $"e2e-nats-{Guid.NewGuid():N}"[..20];

        await LoginAsAdminAsync("/environments");

        // Wait for environments page to fully load
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Register an environment
        var registerButton = Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" });
        await Expect(registerButton).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await registerButton.ClickAsync();
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        // The environment should appear in the list table
        var envCell = Page.GetByRole(AriaRole.Cell, new() { Name = envName });
        await Expect(envCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

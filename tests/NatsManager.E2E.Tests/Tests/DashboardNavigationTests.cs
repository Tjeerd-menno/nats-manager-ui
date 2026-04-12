using FluentAssertions;
using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for dashboard navigation and sidebar routing.
/// </summary>
public sealed class DashboardNavigationTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Dashboard_LoadsAfterLogin()
    {
        await LoginAsAdminAsync("/dashboard");

        // Dashboard page should be visible
        var heading = Page.GetByRole(AriaRole.Heading, new() { Name = "Dashboard" });
        await Expect(heading).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task Sidebar_DisplaysAllNavigationItems()
    {
        await LoginAsAdminAsync("/dashboard");

        // Verify all main navigation items are visible
        await Expect(Page.GetByText("Dashboard", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Environments", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("JetStream", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Key-Value", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Object Store", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Services", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Core NATS", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Audit Log", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Sidebar_CanNavigateToJetStream()
    {
        await LoginAsAdminAsync("/dashboard");

        await Page.GetByText("JetStream", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/jetstream/streams", new PageWaitForURLOptions { Timeout = 10_000 });
        Page.Url.Should().Contain("/jetstream/streams");
    }

    [Fact]
    public async Task Sidebar_CanNavigateToKeyValue()
    {
        await LoginAsAdminAsync("/dashboard");

        await Page.GetByText("Key-Value", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/kv/buckets", new PageWaitForURLOptions { Timeout = 10_000 });
        Page.Url.Should().Contain("/kv/buckets");
    }

    [Fact]
    public async Task Sidebar_CanNavigateToAuditLog()
    {
        await LoginAsAdminAsync("/dashboard");

        await Page.GetByText("Audit Log", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/audit", new PageWaitForURLOptions { Timeout = 10_000 });
        Page.Url.Should().Contain("/audit");
    }

    [Fact]
    public async Task Header_ShowsUserDisplayName()
    {
        await LoginAsAdminAsync("/dashboard");

        // The header should show the admin user's display name
        var userDisplay = Page.GetByText("Administrator");
        await Expect(userDisplay).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task Header_ShowsNatsManagerBranding()
    {
        await LoginAsAdminAsync("/dashboard");

        var branding = Page.GetByText("NATS Manager").First;
        await Expect(branding).ToBeVisibleAsync();
    }
}

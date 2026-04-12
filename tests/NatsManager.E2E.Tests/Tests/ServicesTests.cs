using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Services discovery page using a real NATS instance.
/// </summary>
public sealed class ServicesTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task ServicesPage_ShowsNoEnvironmentMessage_WhenNoEnvironmentSelected()
    {
        await LoginAsAdminAsync("/services");

        await Expect(Page.GetByText("Select an environment to view services"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task ServicesPage_ShowsEmptyState_WhenNoServicesRegistered()
    {
        await LoginAndSetupEnvironmentAsync("/services");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Services" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // With a fresh NATS instance, no services should be discovered
        await Expect(Page.GetByText("No services discovered"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

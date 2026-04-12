using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for Access Control (Users and Roles management).
/// </summary>
public sealed class AccessControlTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task UsersPage_ShowsHeading()
    {
        await LoginAsAdminAsync("/admin/users");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task UsersPage_ShowsDefaultAdminUser()
    {
        await LoginAsAdminAsync("/admin/users");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // The seeded admin user should be visible (Exact match to avoid matching 'Administrator')
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "admin", Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanCreateUser()
    {
        var username = $"e2euser{Guid.NewGuid():N}"[..12];

        await LoginAsAdminAsync("/admin/users");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click Create User
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create User" }).ClickAsync();

        // Fill in the form
        await Page.GetByLabel("Username").FillAsync(username);
        await Page.GetByLabel("Display Name").FillAsync($"E2E User {username}");
        await Page.Locator("input[type='password']").First.FillAsync("TestPassword123!");

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Verify the user appears in the list (Exact to avoid matching display name)
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = username, Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AdminSectionVisibleForAdminUser()
    {
        await LoginAsAdminAsync("/dashboard");

        // The Admin section should be visible in the sidebar for administrators
        var sidebar = Page.Locator("nav");
        await Expect(sidebar.GetByText("Admin", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(sidebar.GetByText("Users")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanNavigateToUsersViaAdminSection()
    {
        await LoginAsAdminAsync("/dashboard");

        // Click on Users in the Admin section of the sidebar
        await Page.GetByText("Users").ClickAsync();

        await Page.WaitForURLAsync("**/admin/users", new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Users" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

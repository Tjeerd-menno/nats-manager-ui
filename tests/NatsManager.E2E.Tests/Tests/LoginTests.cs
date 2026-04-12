using FluentAssertions;
using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the login flow.
/// </summary>
public sealed class LoginTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task LoginPage_RendersLoginForm()
    {
        await NavigateAsync("/login");

        var title = Page.GetByText("NATS Manager");
        await Expect(title).ToBeVisibleAsync();

        var usernameInput = Page.GetByLabel("Username");
        await Expect(usernameInput).ToBeVisibleAsync();

        var signInButton = Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" });
        await Expect(signInButton).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_WithValidCredentials_RedirectsToDashboard()
    {
        await NavigateAsync("/login");

        await Page.GetByLabel("Username").FillAsync(AppHostFixture.BootstrapAdminUsername);
        await Page.GetByLabel("Password").First.FillAsync(AppHostFixture.BootstrapAdminPassword);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Should redirect to dashboard after successful login
        await Page.WaitForURLAsync("**/dashboard", new PageWaitForURLOptions { Timeout = 15_000 });
        Page.Url.Should().Contain("/dashboard");
    }

    [Fact]
    public async Task LoginPage_WithInvalidCredentials_ShowsError()
    {
        await NavigateAsync("/login");

        await Page.GetByLabel("Username").FillAsync(AppHostFixture.BootstrapAdminUsername);
        await Page.GetByLabel("Password").First.FillAsync("wrongpassword");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        // Should show an error message
        var errorAlert = Page.GetByText("Login failed", new PageGetByTextOptions { Exact = false });
        await Expect(errorAlert).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Should stay on login page
        Page.Url.Should().Contain("/login");
    }

    [Fact]
    public async Task UnauthenticatedUser_IsRedirectedToLogin()
    {
        await NavigateAsync("/dashboard");

        // Should redirect unauthenticated users to the login page
        await Page.WaitForURLAsync("**/login", new PageWaitForURLOptions { Timeout = 10_000 });
        Page.Url.Should().Contain("/login");
    }
}

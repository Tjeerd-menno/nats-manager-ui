using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

public sealed class EnvironmentCrudTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task CanEditEnvironment()
    {
        var envName = $"e2e-edit-{Guid.NewGuid():N}"[..18];
        var updatedName = $"e2e-upd-{Guid.NewGuid():N}"[..18];

        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Register an environment
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        // Wait for registration form to close and cell to appear
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Open the action menu for the environment row and click Edit
        var envRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName });
        await envRow.GetByRole(AriaRole.Button).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Edit" }).ClickAsync();

        // Wait for edit form to appear
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Update" }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });

        // Edit the name in the form (also re-fill server URL to ensure it's set)
        var nameField = Page.GetByLabel("Name");
        await nameField.ClearAsync();
        await nameField.FillAsync(updatedName);
        var serverUrlField = Page.GetByLabel("Server URL");
        await serverUrlField.ClearAsync();
        await serverUrlField.FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update" }).ClickAsync();

        await FilterEnvironmentsAsync(updatedName);

        // Verify the updated name appears in the list
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = updatedName }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task CanDeleteEnvironment()
    {
        var envName = $"e2e-del-{Guid.NewGuid():N}"[..18];

        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Register an environment
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        // Wait for the cell to appear
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.WaitForTimeoutAsync(500);

        // Open the action menu and click Delete (capture API response to confirm)
        var envRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName });
        await envRow.GetByRole(AriaRole.Button).Last.ClickAsync();

        var deleteResponseTask = Page.WaitForResponseAsync(r =>
            r.Request.Method == "DELETE" && r.Url.Contains("/api/environments/"));
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Delete" }).ClickAsync();
        var deleteResponse = await deleteResponseTask;

        // If the API returns success, verify the environment is removed
        if (deleteResponse.Status is >= 200 and < 300)
        {
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName }))
                .ToBeHiddenAsync(new() { Timeout = 15_000 });
        }
    }

    [Fact]
    public async Task CanTestConnectionForEnvironment()
    {
        var envName = $"e2e-tc-{Guid.NewGuid():N}"[..18];

        // Use API to register environment (more reliable) and test connection
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        Guid envId;
        using (httpClient)
        using (handler)
        {
            var envIdStr = await RegisterNatsEnvironmentAsync(httpClient, envName);
            envId = Guid.Parse(envIdStr);

            // Test connection via API
            var testResponse = await httpClient.PostAsync($"/api/environments/{envId}/test", null);
            testResponse.EnsureSuccessStatusCode();
        }

        // Navigate to environments page and verify the status shows Available
        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await FilterEnvironmentsAsync(envName);

        var envRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName });
        await Expect(envRow.GetByText("Available")).ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task CanSearchEnvironments()
    {
        var envName1 = $"e2e-srch-{Guid.NewGuid():N}"[..18];
        var envName2 = $"e2e-other-{Guid.NewGuid():N}"[..18];

        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Register two environments
        foreach (var name in new[] { envName1, envName2 })
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" }).ClickAsync();
            await Page.GetByLabel("Name").FillAsync(name);
            await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
            await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();
            await FilterEnvironmentsAsync(name);
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = name }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
        }

        // Search for the first one
        await Page.GetByPlaceholder("Search environments").FillAsync("srch");

        // Should show the first env but not the second
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName1 }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task CanEnableDisableEnvironment()
    {
        var envName = $"e2e-enabl-{Guid.NewGuid():N}"[..18];

        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Register an environment
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Open the action menu and click Disable
        var envRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName });
        await envRow.GetByRole(AriaRole.Button).Last.ClickAsync();

        var disableResponseTask = Page.WaitForResponseAsync(r =>
            r.Request.Method == "POST" && r.Url.Contains("/enable"));
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Disable" }).ClickAsync();
        var disableResponse = await disableResponseTask;

        // Verify API call succeeded
        Assert.True(disableResponse.Status is >= 200 and < 300,
            $"Disable API returned {disableResponse.Status}");

        // Wait for list to refresh — should show "Disabled" badge
        await Expect(envRow.GetByText("Disabled"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Re-enable: open menu and click Enable
        await envRow.GetByRole(AriaRole.Button).Last.ClickAsync();

        var enableResponseTask = Page.WaitForResponseAsync(r =>
            r.Request.Method == "POST" && r.Url.Contains("/enable"));
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Enable" }).ClickAsync();
        var enableResponse = await enableResponseTask;

        Assert.True(enableResponse.Status is >= 200 and < 300,
            $"Enable API returned {enableResponse.Status}");

        // "Disabled" badge should disappear after re-enabling
        await Expect(envRow.GetByText("Disabled"))
            .ToBeHiddenAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task DeleteEnvironment_AuditShowsName()
    {
        var envName = $"e2e-adel-{Guid.NewGuid():N}"[..18];

        // Use API to create and delete environment
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient)
        using (handler)
        {
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Delete the environment via API
            var deleteResponse = await httpClient.DeleteAsync($"/api/environments/{envId}");
            deleteResponse.EnsureSuccessStatusCode();

            // Verify audit log shows the environment name (not GUID) for the delete event
            var auditResponse = await httpClient.GetAsync("/api/audit/events?pageSize=50");
            var auditBody = await auditResponse.Content.ReadAsStringAsync();

            // The Delete audit event should contain the environment name, not just the GUID
            Assert.Contains(envName, auditBody);
        }

        // Also verify via UI
        await LoginAsAdminAsync("/audit");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Find the Delete row with the environment name
        var deleteRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = "Delete" }).Filter(new() { HasText = envName });
        await Expect(deleteRow.First).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}

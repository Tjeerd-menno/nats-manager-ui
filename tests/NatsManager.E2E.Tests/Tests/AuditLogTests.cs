using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for the Audit Log page.
/// </summary>
public sealed class AuditLogTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task AuditPage_ShowsHeading()
    {
        await LoginAsAdminAsync("/audit");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AuditPage_ShowsLoginEvent()
    {
        // Register an environment via API to generate an auditable event
        var envName = $"e2e-alog-{Guid.NewGuid():N}"[..18];
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Verify the audit event exists via API
            var auditResponse = await httpClient.GetAsync("/api/audit/events?pageSize=100");
            var auditBody = await auditResponse.Content.ReadAsStringAsync();

            // If audit events are empty, skip the UI check
            if (!auditBody.Contains(envName))
            {
                throw new Xunit.Sdk.XunitException(
                    $"Audit events API does not contain environment name '{envName}'. " +
                    $"Response: {auditBody[..System.Math.Min(500, auditBody.Length)]}");
            }
        }

        // Now verify via UI
        await LoginAsAdminAsync("/audit");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Should see the environment name in the audit table (Create event)
        await Expect(Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName }).First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AuditPage_CanFilterByActionType()
    {
        await LoginAsAdminAsync("/audit");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Use the action type filter
        var actionFilter = Page.GetByPlaceholder("Action type");
        await actionFilter.ClickAsync();
        await Page.GetByText("Login", new() { Exact = true }).ClickAsync();

        // The table should update (may show events or "No audit events found")
        await Page.WaitForTimeoutAsync(1000);
    }

    [Fact]
    public async Task AuditPage_CanFilterByResourceType()
    {
        await LoginAsAdminAsync("/audit");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Use the resource type filter
        var resourceFilter = Page.GetByPlaceholder("Resource type");
        await resourceFilter.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Environment" }).ClickAsync();

        // The table should update
        await Page.WaitForTimeoutAsync(1000);
    }

    [Fact]
    public async Task AuditPage_ShowsEnvironmentCreateEvent_AfterRegistration()
    {
        // Register an environment first to generate an audit event
        var envName = $"e2e-audit-{Guid.NewGuid():N}"[..18];

        await LoginAsAdminAsync("/environments");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Environments" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        await Page.GetByRole(AriaRole.Button, new() { Name = "Register Environment" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(envName);
        await Page.GetByLabel("Server URL").FillAsync(Fixture.NatsUrl);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();

        await FilterEnvironmentsAsync(envName);

        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = envName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        int? eventPage = null;
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient)
        using (handler)
        {
            var totalPages = 1;
            for (var page = 1; page <= totalPages; page++)
            {
                var response = await httpClient.GetAsync($"/api/audit/events?page={page}&pageSize=50");
                response.EnsureSuccessStatusCode();

                var payload = await response.Content.ReadAsStringAsync();
                using var document = System.Text.Json.JsonDocument.Parse(payload);
                var root = document.RootElement;
                var totalCount = root.GetProperty("totalCount").GetInt32();
                var pageSize = root.GetProperty("pageSize").GetInt32();
                totalPages = Math.Max(totalPages, (int)Math.Ceiling(totalCount / (double)pageSize));

                if (!payload.Contains(envName, StringComparison.Ordinal))
                {
                    continue;
                }

                eventPage = page;
                break;
            }
        }

        Assert.NotNull(eventPage);

        // Navigate to audit log via sidebar (SPA navigation)
        await Page.GetByText("Audit Log", new PageGetByTextOptions { Exact = true }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*\\/audit"),
            new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Audit Log" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        if (eventPage > 1)
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = eventPage.Value.ToString() }).ClickAsync();
        }

        // Should see the environment name in the audit table (Create event)
        await Expect(Page.GetByRole(AriaRole.Row).Filter(new() { HasText = envName }).First)
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task AuditEvents_ShowActualActorName_NotSystem()
    {
        // Perform an action as admin and verify the actor is "admin", not "System"
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-actor-{Guid.NewGuid():N}"[..18];
            await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Fetch audit events and find the one for our environment
            var auditResponse = await httpClient.GetAsync("/api/audit/events?pageSize=100");
            auditResponse.EnsureSuccessStatusCode();
            var auditBody = await auditResponse.Content.ReadAsStringAsync();

            // The audit event for creating the environment should have actor "Administrator" (display name), not "System"
            var doc = System.Text.Json.JsonDocument.Parse(auditBody);
            var events = doc.RootElement.GetProperty("items");

            bool foundWithCorrectActor = false;
            foreach (var evt in events.EnumerateArray())
            {
                var resourceName = evt.GetProperty("resourceName").GetString() ?? "";
                if (resourceName.Contains(envName))
                {
                    var actorName = evt.GetProperty("actorName").GetString();
                    Assert.Equal("Administrator", actorName);
                    foundWithCorrectActor = true;
                    break;
                }
            }

            Assert.True(foundWithCorrectActor, $"Did not find audit event with environment name '{envName}'");
        }
    }
}

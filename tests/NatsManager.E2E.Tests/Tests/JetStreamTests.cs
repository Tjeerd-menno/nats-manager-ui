using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for JetStream stream and consumer management using a real NATS instance.
/// </summary>
public sealed class JetStreamTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task StreamsPage_ShowsNoEnvironmentMessage_WhenNoEnvironmentSelected()
    {
        await LoginAsAdminAsync("/jetstream/streams");

        await Expect(Page.GetByText("Select an environment to view streams"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanCreateStream()
    {
        var streamName = $"E2E-STREAM-{Guid.NewGuid():N}"[..20];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Wait for the streams page heading
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click Create Stream
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();

        // Fill in the stream form
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput.FillAsync($"{streamName}.>");
        await subjectsInput.PressAsync("Enter");

        // Submit the form
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Verify the stream appears in the list
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanViewStreamDetail()
    {
        var streamName = $"E2E-DETAIL-{Guid.NewGuid():N}"[..20];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create a stream first
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput2 = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput2.FillAsync($"{streamName}.>");
        await subjectsInput2.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Wait for stream to appear and click on it
        var streamCell = Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true });
        await Expect(streamCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await streamCell.ClickAsync();

        // Should navigate to stream detail
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });

        // Verify stream detail elements are visible
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("Messages")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("Consumers")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanDeleteStream()
    {
        var streamName = $"E2E-DEL-{Guid.NewGuid():N}"[..18];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create a stream
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput3 = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput3.FillAsync($"{streamName}.>");
        await subjectsInput3.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Navigate to stream detail
        var streamCell = Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true });
        await Expect(streamCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await streamCell.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });

        // Click Delete button on the stream detail page
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        // Confirm deletion in the modal
        var confirmDeleteBtn = Page.Locator(".mantine-Modal-content").GetByRole(AriaRole.Button, new() { Name = "Delete" });
        await Expect(confirmDeleteBtn).ToBeVisibleAsync(new() { Timeout = 5_000 });
        await confirmDeleteBtn.ClickAsync();

        // Should navigate back to stream list
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanPurgeStream()
    {
        var streamName = $"E2E-PURGE-{Guid.NewGuid():N}"[..18];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create a stream
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput4 = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput4.FillAsync($"{streamName}.>");
        await subjectsInput4.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Navigate to stream detail
        var streamCell = Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true });
        await Expect(streamCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await streamCell.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });

        // Click Purge button
        await Page.GetByRole(AriaRole.Button, new() { Name = "Purge" }).ClickAsync();

        // Confirm purge in the modal
        var confirmPurgeBtn = Page.Locator(".mantine-Modal-content").GetByRole(AriaRole.Button, new() { Name = "Purge" });
        await Expect(confirmPurgeBtn).ToBeVisibleAsync(new() { Timeout = 5_000 });
        await confirmPurgeBtn.ClickAsync();

        // Should stay on the stream detail page
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanCreateConsumer()
    {
        var streamName = $"E2ECONS-{Guid.NewGuid():N}"[..16];
        var consumerName = $"e2e-consumer-{Guid.NewGuid():N}"[..20];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create a stream first
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput5 = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput5.FillAsync($"{streamName}.>");
        await subjectsInput5.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Navigate to stream detail
        var streamCell = Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true });
        await Expect(streamCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await streamCell.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });

        // Click Add Consumer
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Consumer" }).ClickAsync();

        // Wait for the consumer form modal to appear
        var dialog = Page.GetByRole(AriaRole.Dialog);
        await Expect(dialog).ToBeVisibleAsync(new() { Timeout = 5_000 });

        // Fill in the consumer form inside the dialog
        await dialog.GetByLabel("Name").FillAsync(consumerName);

        // Submit and capture the API response
        var createResponseTask = Page.WaitForResponseAsync(r =>
            r.Url.Contains("/consumers") && r.Request.Method == "POST");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
        var createResponse = await createResponseTask;

        // Assert the creation was successful
        if (createResponse.Status is not (>= 200 and < 300))
        {
            var body = await createResponse.TextAsync();
            throw new Xunit.Sdk.XunitException(
                $"Consumer creation failed with status {createResponse.Status}: {body}");
        }

        // Wait for the dialog to close (successful submission)
        await Expect(dialog).ToBeHiddenAsync(new() { Timeout = 10_000 });

        // Verify consumer appears in the consumers table
        await Expect(Page.GetByText(consumerName, new PageGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanSearchStreams()
    {
        var streamPrefix = $"E2ESRCH{Guid.NewGuid():N}"[..10];
        var streamName = $"{streamPrefix}-STREAM";

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create a stream
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput6 = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput6.FillAsync($"{streamName}.>");
        await subjectsInput6.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Search for the stream by its unique prefix
        await Page.GetByPlaceholder("Search streams").FillAsync(streamPrefix);

        // The stream should still be visible
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    [Fact]
    public async Task StreamsList_DoesNotShowInternalKvOrObjStreams()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Create a KV bucket (creates an internal KV_ stream)
            var kvBucketName = $"e2e-int-{Guid.NewGuid():N}"[..16];
            var kvResponse = await httpClient.PostAsync($"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{kvBucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));
            kvResponse.EnsureSuccessStatusCode();

            // Fetch the streams list via API
            var streamsResponse = await httpClient.GetAsync($"/api/environments/{envId}/jetstream/streams");
            streamsResponse.EnsureSuccessStatusCode();
            var streamsBody = await streamsResponse.Content.ReadAsStringAsync();

            // The internal KV_ stream should NOT appear in the list
            Assert.DoesNotContain($"KV_{kvBucketName}", streamsBody);
        }
    }

    [Fact]
    public async Task EmptyStreamDetail_DoesNotShowError()
    {
        var streamName = $"E2E-EMPTY-{Guid.NewGuid():N}"[..20];

        await LoginAndSetupEnvironmentAsync("/jetstream/streams");

        // Create an empty stream
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "JetStream Streams" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Stream" }).ClickAsync();
        await Page.GetByLabel("Name").FillAsync(streamName);
        var subjectsInput = Page.GetByPlaceholder("Type a subject and press Enter");
        await subjectsInput.FillAsync($"{streamName}.>");
        await subjectsInput.PressAsync("Enter");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Navigate to stream detail
        var streamCell = Page.GetByRole(AriaRole.Cell, new() { Name = streamName, Exact = true });
        await Expect(streamCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await streamCell.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*jetstream/streams/{streamName}"),
            new() { Timeout = 10_000 });

        // The stream detail should load without error - verify Messages tab is visible
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("Messages", new() { Exact = true })).ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Verify no error notification is shown
        var errorNotification = Page.Locator(".mantine-Notification-root").Filter(new() { HasText = "error" });
        await Expect(errorNotification).ToHaveCountAsync(0);
    }
}

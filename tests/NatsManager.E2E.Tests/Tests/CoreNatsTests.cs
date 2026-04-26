using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for Core NATS server info and message publishing using a real NATS instance.
/// </summary>
public sealed class CoreNatsTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task CoreNatsPage_ShowsNoEnvironmentMessage_WhenNoEnvironmentSelected()
    {
        await LoginAsAdminAsync("/core-nats");

        await Expect(Page.GetByText("Select an environment to view NATS server info"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CoreNatsPage_ShowsServerInfo()
    {
        await LoginAndSetupEnvironmentAsync("/core-nats");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Core NATS" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Should show server info cards (scope to main content to avoid sidebar nav matches)
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("Server")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("Connections")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Main).GetByText("JetStream")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanPublishMessage()
    {
        await LoginAndSetupEnvironmentAsync("/core-nats");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Core NATS" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click Publish Message button
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish Message" }).ClickAsync();

        // Fill in the publish form (use placeholder to avoid ambiguity with the LiveMessageViewer's "Subject pattern" input)
        await Page.GetByPlaceholder("e.g. orders.created").FillAsync("test.e2e.subject");
        await Page.GetByLabel("Payload").FillAsync("Hello from E2E tests!");

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish", Exact = true }).ClickAsync();

        // Should show success message
        await Expect(Page.GetByText("Message published successfully"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanGetServerStatusViaApi()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/status");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            Assert.False(string.IsNullOrEmpty(root.GetProperty("serverId").GetString()));
            Assert.False(string.IsNullOrEmpty(root.GetProperty("version").GetString()));
            Assert.True(root.GetProperty("port").GetInt32() > 0);
            Assert.True(root.GetProperty("maxPayload").GetInt64() > 0);
        }
    }

    [Fact]
    public async Task CanPublishMessageViaApi()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                new StringContent("""{"subject":"test.e2e.api","payload":"Hello from API test"}""",
                    Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.GetProperty("published").GetBoolean());
        }
    }

    [Fact]
    public async Task CanPublishMessage_WithHeadersAndReplyTo()
    {
        await LoginAndSetupEnvironmentAsync("/core-nats");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish Message" }).ClickAsync();
        await Page.GetByPlaceholder("e.g. orders.created").FillAsync("test.e2e.headers");
        await Page.GetByText("JSON").ClickAsync();
        await Page.GetByLabel("Payload").FillAsync("""{"hello":"world"}""");
        await Page.GetByLabel("Reply-To (optional)").FillAsync("test.e2e.reply");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add Header" }).ClickAsync();
        await Page.GetByPlaceholder("Key", new() { Exact = true }).FillAsync("X-E2E");
        await Page.GetByPlaceholder("Value").FillAsync("true");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish", Exact = true }).ClickAsync();

        await Expect(Page.GetByText("Message published successfully"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanPublishMessage_WithJsonAndHeaders_ViaApi()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                JsonContent("""
                {
                  "subject": "test.e2e.json",
                  "payload": "{\"orderId\":\"abc\"}",
                  "payloadFormat": "Json",
                  "headers": { "X-Source": "e2e" },
                  "replyTo": "test.e2e.reply"
                }
                """));
            response.EnsureSuccessStatusCode();

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("published").GetBoolean());
        }
    }

    [Fact]
    public async Task CanPublishMessage_ViaApi_WithHexBytesFormat()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                JsonContent("""{"subject":"test.e2e.hex","payload":"48656c6c6f","payloadFormat":"HexBytes"}"""));
            response.EnsureSuccessStatusCode();

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(doc.RootElement.GetProperty("published").GetBoolean());
        }
    }

    [Fact]
    public async Task PublishWithEmptySubject_Returns422()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                new StringContent("""{"subject":"","payload":"test"}""",
                    Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }
    }

    [Fact]
    public async Task PublishWithNullPayload_Succeeds()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                new StringContent("""{"subject":"test.null.payload"}""",
                    Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.GetProperty("published").GetBoolean());
        }
    }

    [Fact]
    public async Task GetSubjectsViaApi_ReturnsEmptyList()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/subjects");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }
    }

    [Fact]
    public async Task GetSubjectsViaApi_ReturnsSubjectsWhenSubscriberActive()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);
            var subject = $"test.e2e.subjects.{Guid.NewGuid():N}";
            using var streamCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var streamTask = httpClient.GetAsync(
                $"/api/environments/{envId}/core-nats/stream?subject={Uri.EscapeDataString(subject)}",
                HttpCompletionOption.ResponseHeadersRead,
                streamCts.Token);
            await Task.Delay(500, streamCts.Token);
            await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                JsonContent($$"""{"subject":"{{subject}}","payload":"wake subscriber"}"""),
                streamCts.Token);

            var streamResponse = await streamTask.WaitAsync(streamCts.Token);
            streamResponse.EnsureSuccessStatusCode();

            var response = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/subjects", streamCts.Token);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(streamCts.Token);
            var doc = JsonDocument.Parse(body);
            Assert.Contains(doc.RootElement.EnumerateArray(), item =>
                item.GetProperty("subject").GetString() == subject);

            await streamCts.CancelAsync();
            streamResponse.Dispose();
        }
    }

    [Fact]
    public async Task StreamEndpoint_Returns400_ForEmptySubject()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/stream");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task GetClientsViaApi_ReturnsEmptyList()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var response = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/clients");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
            Assert.Equal(0, doc.RootElement.GetArrayLength());
        }
    }

    [Fact]
    public async Task StatusForNonExistentEnvironment_Returns404()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var fakeEnvId = Guid.NewGuid();

            var response = await httpClient.GetAsync($"/api/environments/{fakeEnvId}/core-nats/status");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task PublishMessage_ThenStatusStillReturnsValidInfo()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Publish a message
            var publishResponse = await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                new StringContent("""{"subject":"test.stats.count","payload":"counting"}""",
                    Encoding.UTF8, "application/json"));
            publishResponse.EnsureSuccessStatusCode();

            var publishDoc = JsonDocument.Parse(await publishResponse.Content.ReadAsStringAsync());
            Assert.True(publishDoc.RootElement.GetProperty("published").GetBoolean());

            // Status endpoint should still return valid server info after publish
            var statusResponse = await httpClient.GetAsync($"/api/environments/{envId}/core-nats/status");
            statusResponse.EnsureSuccessStatusCode();

            var statusDoc = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
            Assert.False(string.IsNullOrEmpty(statusDoc.RootElement.GetProperty("serverId").GetString()));
            Assert.True(statusDoc.RootElement.GetProperty("port").GetInt32() > 0);
        }
    }

    [Fact]
    public async Task CoreNatsPage_ShowsSubjectTable_WhenSubscriberActive()
    {
        var envId = await LoginAndSetupEnvironmentAsync("/core-nats");
        var subject = $"test.e2e.ui.{Guid.NewGuid():N}";

        await Page.GetByLabel("Subject pattern").FillAsync(subject);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Subscribe" }).ClickAsync();

        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                JsonContent($$"""{"subject":"{{subject}}","payload":"hello table"}"""));
        }

        await Expect(Page.GetByRole(AriaRole.Main).GetByText(subject))
            .ToBeVisibleAsync(new() { Timeout = 20_000 });
    }

    [Fact]
    public async Task SubjectFilter_ReducesVisibleRows()
    {
        var envId = await LoginAndSetupEnvironmentAsync("/core-nats");
        var subject = $"test.e2e.filter.{Guid.NewGuid():N}";

        await Page.GetByLabel("Subject pattern").FillAsync(subject);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Subscribe" }).ClickAsync();

        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            await httpClient.PostAsync(
                $"/api/environments/{envId}/core-nats/publish",
                JsonContent($$"""{"subject":"{{subject}}","payload":"hello filter"}"""));
        }

        await Expect(Page.GetByRole(AriaRole.Main).GetByText(subject))
            .ToBeVisibleAsync(new() { Timeout = 20_000 });

        await Page.GetByLabel("Filter subjects").FillAsync("does.not.match");

        await Expect(Page.GetByText("No subjects match your filter"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task LiveViewer_ReceivesPublishedMessage()
    {
        await LoginAndSetupEnvironmentAsync("/core-nats");
        var subject = $"test.e2e.live.{Guid.NewGuid():N}";

        await Page.GetByLabel("Subject pattern").FillAsync(subject);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Subscribe" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish Message" }).ClickAsync();
        await Page.GetByPlaceholder("e.g. orders.created").FillAsync(subject);
        await Page.GetByLabel("Payload").FillAsync("Hello live viewer");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Publish", Exact = true }).ClickAsync();
        await Expect(Page.GetByText("Message published successfully"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.GetByRole(AriaRole.Main).GetByText(subject))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Expect(Page.GetByRole(AriaRole.Table).GetByText("Hello live viewer"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task PublishToNonExistentEnvironment_Returns404()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var fakeEnvId = Guid.NewGuid();

            var response = await httpClient.PostAsync(
                $"/api/environments/{fakeEnvId}/core-nats/publish",
                new StringContent("""{"subject":"test","payload":"should fail"}""",
                    Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    private static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");
}

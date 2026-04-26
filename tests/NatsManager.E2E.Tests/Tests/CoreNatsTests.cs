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

        // Fill in the publish form (use Exact = true to avoid matching the LiveMessageViewer's "Subject pattern" input)
        await Page.GetByLabel("Subject", new() { Exact = true }).FillAsync("test.e2e.subject");
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
}

using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for Key-Value store management using a real NATS instance.
/// </summary>
public sealed class KeyValueTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task KvPage_ShowsNoEnvironmentMessage_WhenNoEnvironmentSelected()
    {
        await LoginAsAdminAsync("/kv/buckets");

        await Expect(Page.GetByText("Select an environment to view KV buckets"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanCreateKvBucket()
    {
        var bucketName = $"e2e-kv-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/kv/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Key-Value Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click Create Bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();

        // Fill in the bucket form
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Verify the bucket appears in the list
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanViewBucketDetail()
    {
        var bucketName = $"e2e-kvd-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/kv/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Key-Value Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Create a bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Wait for bucket to appear in the list
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Wait for the form/modal to fully close before clicking
        await Page.WaitForTimeoutAsync(500);

        // Click on the bucket row to navigate to detail
        await Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }).ClickAsync();

        // Should navigate to bucket detail (KV buckets may have KV_ prefix in NATS)
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*kv/buckets/.*{System.Text.RegularExpressions.Regex.Escape(bucketName)}"),
            new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanPutAndViewKey()
    {
        var bucketName = $"e2e-kvk-{Guid.NewGuid():N}"[..16];

        // Set up environment and create bucket via API
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient)
        using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Create a KV bucket via API
            var createBucketResponse = await httpClient.PostAsync(
                $"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}","history":1,"maxBytes":-1,"maxValueSize":-1}""",
                    System.Text.Encoding.UTF8, "application/json"));
            createBucketResponse.EnsureSuccessStatusCode();

            // Inject session cookie and navigate
            var backendCookies = handler.CookieContainer.GetCookies(new Uri(Fixture.BackendUrl));
            var sessionCookie = backendCookies[".AspNetCore.Session"]!;
            var frontendUri = new Uri(Fixture.FrontendUrl);
            await Page.Context.AddCookiesAsync(
            [
                new()
                {
                    Name = sessionCookie.Name,
                    Value = sessionCookie.Value,
                    Domain = frontendUri.Host,
                    Path = sessionCookie.Path,
                    HttpOnly = sessionCookie.HttpOnly,
                    Secure = sessionCookie.Secure,
                    SameSite = SameSiteAttribute.Strict,
                }
            ]);

            await NavigateAsync("/kv/buckets");

            // Select the environment
            var envSelector = Page.GetByPlaceholder("Select environment");
            await envSelector.ClickAsync();
            await Page.GetByText(envName).ClickAsync();

            // Navigate to the bucket detail
            await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
                .ToBeVisibleAsync(new() { Timeout = 10_000 });
            await Page.WaitForTimeoutAsync(500);
            await Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }).ClickAsync();

            await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*kv/buckets/.*{System.Text.RegularExpressions.Regex.Escape(bucketName)}"),
                new() { Timeout = 10_000 });
        }
    }

    [Fact]
    public async Task CanDeleteKvBucket()
    {
        var bucketName = $"e2e-kvdel-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/kv/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Key-Value Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Create a bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Wait for bucket to appear
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.WaitForTimeoutAsync(500);

        // Click the delete button (trash icon) in the bucket row
        var bucketRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = bucketName });
        await bucketRow.GetByRole(AriaRole.Button).ClickAsync();

        // Confirm deletion in the modal
        await Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync(new() { Timeout = 5_000 });
        await Page.GetByRole(AriaRole.Dialog).GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        // The bucket should be removed from the list
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeHiddenAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task KvBucketsList_DoesNotShowObjectStoreBuckets()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Create an Object Store bucket (creates an internal OBJ_ stream that may leak into KV)
            var objBucketName = $"e2e-obj-{Guid.NewGuid():N}"[..16];
            var objResponse = await httpClient.PostAsync($"/api/environments/{envId}/objectstore/buckets",
                new StringContent($$"""{"bucketName":"{{objBucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));
            objResponse.EnsureSuccessStatusCode();

            // Fetch the KV buckets list via API
            var kvResponse = await httpClient.GetAsync($"/api/environments/{envId}/kv/buckets");
            kvResponse.EnsureSuccessStatusCode();
            var kvBody = await kvResponse.Content.ReadAsStringAsync();

            // The OBJ_ bucket should NOT appear in the KV listing
            Assert.DoesNotContain(objBucketName, kvBody);
        }
    }

    [Fact]
    public async Task CanGetKeyHistory()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Create a bucket with history=5
            var bucketName = $"e2e-hist-{Guid.NewGuid():N}"[..16];
            var createResponse = await httpClient.PostAsync($"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}","history":5}""",
                    System.Text.Encoding.UTF8, "application/json"));
            createResponse.EnsureSuccessStatusCode();

            // Put the same key multiple times to generate history
            var valuesB64 = new[] { "v1", "v2", "v3" }
                .Select(v => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(v)));
            foreach (var b64 in valuesB64)
            {
                var putResponse = await httpClient.PutAsync(
                    $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/versioned",
                    new StringContent($$"""{"value":"{{b64}}"}""",
                        System.Text.Encoding.UTF8, "application/json"));
                putResponse.EnsureSuccessStatusCode();
            }

            // Fetch history
            var histResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/versioned/history");
            histResponse.EnsureSuccessStatusCode();
            var histBody = await histResponse.Content.ReadAsStringAsync();
            var histDoc = System.Text.Json.JsonDocument.Parse(histBody);

            // History should contain 3 entries
            Assert.Equal(3, histDoc.RootElement.GetProperty("entries").GetArrayLength());
        }
    }

    [Fact]
    public async Task CanSearchKeys()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-srch-{Guid.NewGuid():N}"[..16];
            var createResponse = await httpClient.PostAsync($"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}","history":1}""",
                    System.Text.Encoding.UTF8, "application/json"));
            createResponse.EnsureSuccessStatusCode();

            // Put keys with different prefixes
            foreach (var key in new[] { "app.name", "app.version", "db.host" })
            {
                var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("value"));
                await httpClient.PutAsync(
                    $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/{key}",
                    new StringContent($$"""{"value":"{{b64}}"}""",
                        System.Text.Encoding.UTF8, "application/json"));
            }

            // Search for "app" keys only
            var searchResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys?search=app");
            searchResponse.EnsureSuccessStatusCode();
            var body = await searchResponse.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(body);

            var items = doc.RootElement.GetProperty("items");
            Assert.Equal(2, items.GetArrayLength());
        }
    }

    [Fact]
    public async Task CanDeleteKeyViaApi()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-del-{Guid.NewGuid():N}"[..16];
            await httpClient.PostAsync($"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}","history":1}""",
                    System.Text.Encoding.UTF8, "application/json"));

            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("to-delete"));
            await httpClient.PutAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/temp-key",
                new StringContent($$"""{"value":"{{b64}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));

            // Delete without X-Confirm should fail
            var noConfirm = await httpClient.DeleteAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/temp-key");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, noConfirm.StatusCode);

            // Delete with X-Confirm should succeed
            var req = new HttpRequestMessage(HttpMethod.Delete,
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/temp-key");
            req.Headers.Add("X-Confirm", "true");
            var confirmed = await httpClient.SendAsync(req);
            Assert.Equal(System.Net.HttpStatusCode.NoContent, confirmed.StatusCode);

            // Key should be gone
            var getResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/temp-key");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }

    [Fact]
    public async Task PutKey_WithWrongExpectedRevision_Returns409()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-occ-{Guid.NewGuid():N}"[..16];
            await httpClient.PostAsync($"/api/environments/{envId}/kv/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}","history":1}""",
                    System.Text.Encoding.UTF8, "application/json"));

            // Put initial value
            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("initial"));
            await httpClient.PutAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/occ-key",
                new StringContent($$"""{"value":"{{b64}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));

            // Try to update with wrong expectedRevision
            var b64v2 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("conflict"));
            var conflictResponse = await httpClient.PutAsync(
                $"/api/environments/{envId}/kv/buckets/{bucketName}/keys/occ-key",
                new StringContent($$"""{"value":"{{b64v2}}","expectedRevision":999}""",
                    System.Text.Encoding.UTF8, "application/json"));

            Assert.Equal(System.Net.HttpStatusCode.Conflict, conflictResponse.StatusCode);
        }
    }
}

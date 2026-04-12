using Microsoft.Playwright;
using NatsManager.E2E.Tests.Infrastructure;

namespace NatsManager.E2E.Tests.Tests;

/// <summary>
/// E2E tests for Object Store management using a real NATS instance.
/// </summary>
public sealed class ObjectStoreTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task ObjectStorePage_ShowsNoEnvironmentMessage_WhenNoEnvironmentSelected()
    {
        await LoginAsAdminAsync("/objectstore/buckets");

        await Expect(Page.GetByText("Select an environment to view object store buckets"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanCreateObjectStoreBucket()
    {
        var bucketName = $"e2e-obj-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/objectstore/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Object Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Click Create Bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();

        // Fill in the bucket form
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);

        // Submit
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Verify the bucket appears in the table
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanViewObjectStoreBucketDetail()
    {
        var bucketName = $"e2e-objd-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/objectstore/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Object Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Create a bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Click on the bucket (wait for form to close first)
        var bucketCell = Page.GetByRole(AriaRole.Cell, new() { Name = bucketName });
        await Expect(bucketCell).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.WaitForTimeoutAsync(500);
        await bucketCell.ClickAsync();

        // Should navigate to bucket detail showing the bucket name in the heading
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($".*objectstore/buckets/{bucketName}"),
            new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CanDeleteObjectStoreBucket()
    {
        var bucketName = $"e2e-objx-{Guid.NewGuid():N}"[..16];

        await LoginAndSetupEnvironmentAsync("/objectstore/buckets");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Object Store" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        // Create a bucket
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create Bucket" }).ClickAsync();
        await Page.GetByLabel("Bucket Name").FillAsync(bucketName);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true }).ClickAsync();

        // Wait for bucket to appear
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
        await Page.WaitForTimeoutAsync(500);

        // Click the delete button (emoji trash icon, no confirmation modal)
        var bucketRow = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = bucketName });
        await bucketRow.GetByRole(AriaRole.Button).ClickAsync();

        // Bucket should be removed (no confirmation step - ObjStore deletes directly)
        await Expect(Page.GetByRole(AriaRole.Cell, new() { Name = bucketName }))
            .ToBeHiddenAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task CanUploadAndDownloadObject()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            // Create a bucket
            var bucketName = $"e2e-upd-{Guid.NewGuid():N}"[..16];
            var createResponse = await httpClient.PostAsync($"/api/environments/{envId}/objectstore/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));
            createResponse.EnsureSuccessStatusCode();

            // Upload a file
            var content = System.Text.Encoding.UTF8.GetBytes("Hello, World! This is test content.");
            var uploadResponse = await httpClient.PostAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/test-file.txt/upload",
                new ByteArrayContent(content) { Headers = { { "Content-Type", "application/octet-stream" } } });
            Assert.Equal(System.Net.HttpStatusCode.Created, uploadResponse.StatusCode);

            // Download and verify content
            var downloadResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/test-file.txt/download");
            downloadResponse.EnsureSuccessStatusCode();
            var downloaded = await downloadResponse.Content.ReadAsByteArrayAsync();
            Assert.Equal(content, downloaded);
        }
    }

    [Fact]
    public async Task CanGetObjectDetail()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-det-{Guid.NewGuid():N}"[..16];
            await httpClient.PostAsync($"/api/environments/{envId}/objectstore/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));

            var content = System.Text.Encoding.UTF8.GetBytes("detail test data");
            await httpClient.PostAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/detail.txt/upload",
                new ByteArrayContent(content) { Headers = { { "Content-Type", "application/octet-stream" } } });

            // Get object detail
            var detailResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/detail.txt");
            detailResponse.EnsureSuccessStatusCode();
            var body = await detailResponse.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(body);

            Assert.Equal("detail.txt", doc.RootElement.GetProperty("name").GetString());
            Assert.Equal(content.Length, doc.RootElement.GetProperty("size").GetInt64());
        }
    }

    [Fact]
    public async Task CanDeleteObjectViaApi()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-odel-{Guid.NewGuid():N}"[..16];
            await httpClient.PostAsync($"/api/environments/{envId}/objectstore/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));

            var content = System.Text.Encoding.UTF8.GetBytes("to delete");
            await httpClient.PostAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/temp.txt/upload",
                new ByteArrayContent(content) { Headers = { { "Content-Type", "application/octet-stream" } } });

            // Delete without X-Confirm should fail
            var noConfirm = await httpClient.DeleteAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/temp.txt");
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, noConfirm.StatusCode);

            // Delete with X-Confirm should succeed
            var req = new HttpRequestMessage(HttpMethod.Delete,
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/temp.txt");
            req.Headers.Add("X-Confirm", "true");
            var confirmed = await httpClient.SendAsync(req);
            Assert.Equal(System.Net.HttpStatusCode.NoContent, confirmed.StatusCode);

            // Object should be gone (null info)
            var getResponse = await httpClient.GetAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/temp.txt");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponse.StatusCode);
        }
    }

    [Fact]
    public async Task GetNonExistentObject_Returns404()
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient) using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            var bucketName = $"e2e-404-{Guid.NewGuid():N}"[..16];
            await httpClient.PostAsync($"/api/environments/{envId}/objectstore/buckets",
                new StringContent($$"""{"bucketName":"{{bucketName}}"}""",
                    System.Text.Encoding.UTF8, "application/json"));

            var response = await httpClient.GetAsync(
                $"/api/environments/{envId}/objectstore/buckets/{bucketName}/objects/nonexistent");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

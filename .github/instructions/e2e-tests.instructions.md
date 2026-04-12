---
description: "Use when writing or modifying E2E tests with Playwright and Aspire. Covers E2E fixture setup, test base class, browser automation, and authenticated HTTP client patterns."
applyTo: "tests/NatsManager.E2E.Tests/**"
---
# E2E Test Instructions

## Framework

- **Playwright** (.NET) for browser automation
- **xUnit v3** with assembly-level `AppHostFixture` (starts Aspire app once for all tests)
- Headless Chromium by default
- Tests run sequentially (`DisableTestParallelization = true`)

## Test Base Class

Extend `E2ETestBase` which provides:

- `Page` — Playwright `IPage` instance (fresh context per test)
- `LoginAsAdminAsync(path)` — auth via API, copy cookies to browser, navigate
- `LoginAndSetupEnvironmentAsync(path)` — login + register NATS environment + select it in the UI
- `CreateAuthenticatedHttpClientAsync()` — returns `(HttpClient, Handler)` for API-level tests
- `RegisterNatsEnvironmentAsync(client, name)` — register environment pointing to Aspire NATS container
- `Expect(locator)` — Playwright assertion wrapper

## UI Test Pattern

```csharp
public sealed class MyFeatureTests(AppHostFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task MyFeature_ShowsExpectedContent()
    {
        await LoginAndSetupEnvironmentAsync("/my-feature");

        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Feature" }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }
}
```

## API Test Pattern

```csharp
[Fact]
public async Task MyApi_ReturnsExpectedResult()
{
    var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
    using (httpClient) using (handler)
    {
        var envName = $"e2e-{Guid.NewGuid():N}"[..16];
        var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

        var response = await httpClient.GetAsync($"/api/environments/{envId}/my-resource");
        response.EnsureSuccessStatusCode();
    }
}
```

## Conventions

- Use `Timeout = 10_000` for assertions to account for startup time
- Environment names: `$"e2e-{Guid.NewGuid():N}"[..16]` (unique, short)
- Always `using` the HttpClient and handler from `CreateAuthenticatedHttpClientAsync`
- Scope locators to `Page.GetByRole(AriaRole.Main)` to avoid matching sidebar navigation
- Use `Expect(...)` instead of raw Playwright `ILocatorAssertions`

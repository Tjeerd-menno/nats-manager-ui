using Microsoft.Playwright;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace NatsManager.E2E.Tests.Infrastructure;

/// <summary>
/// Base class for E2E tests. Each test class creates its own Playwright browser instance
/// and an isolated browser context + page per test.
/// </summary>
public abstract class E2ETestBase : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IBrowserContext? context;

    protected AppHostFixture Fixture { get; }
    protected IPage Page { get; private set; } = default!;

    protected E2ETestBase(AppHostFixture fixture)
    {
        Fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new Exception($"Playwright browser install failed with exit code {exitCode}");
        }

        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        Page = await context.NewPageAsync();

        Page.Console += (_, msg) =>
        {
            string msgType = msg.Type.ToUpperInvariant();
            if (msgType is "ERROR" or "WARNING")
            {
                Console.WriteLine($"[BROWSER {msgType}] {msg.Text}");
            }
        };

        Page.PageError += (_, error) =>
        {
            Console.WriteLine($"[BROWSER PAGE ERROR] {error}");
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (Page is not null) await Page.CloseAsync();
        if (context is not null) await context.CloseAsync();
        if (browser is not null) await browser.CloseAsync();
        playwright?.Dispose();
    }

    /// <summary>
    /// Navigate to a path relative to the frontend URL.
    /// </summary>
    protected async Task NavigateAsync(string path = "/")
    {
        await Page.GotoAsync($"{Fixture.FrontendUrl}{path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
        });
    }

    /// <summary>
    /// Log in as the default admin user and navigate to the specified path.
    /// </summary>
    protected async Task LoginAsAdminAsync(string navigateTo = "/dashboard")
    {
        // Login directly via the backend API (bypassing Vite proxy) for reliable session creation
        using var handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer(),
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(Fixture.BackendUrl) };

        // Retry login a few times to handle cold-start transient failures
        HttpResponseMessage loginResponse = null!;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            loginResponse = await httpClient.PostAsync("/api/auth/login",
                new StringContent(
                    $$"""{"username":"{{AppHostFixture.BootstrapAdminUsername}}","password":"{{AppHostFixture.BootstrapAdminPassword}}"}""",
                    Encoding.UTF8,
                    "application/json"));
            if (loginResponse.IsSuccessStatusCode) break;
            await Task.Delay(1000);
        }
        loginResponse.EnsureSuccessStatusCode();

        await InitializeAntiforgeryAsync(httpClient, handler.CookieContainer);

        await CopyBackendCookiesToBrowserAsync(handler.CookieContainer);

        await NavigateAsync(navigateTo);
    }

    /// <summary>
    /// Create an authenticated HttpClient with the admin session cookie.
    /// </summary>
    protected async Task<(HttpClient Client, HttpClientHandler Handler)> CreateAuthenticatedHttpClientAsync()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer(),
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(Fixture.BackendUrl) };

        HttpResponseMessage loginResponse = null!;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            loginResponse = await httpClient.PostAsync("/api/auth/login",
                new StringContent(
                    $$"""{"username":"{{AppHostFixture.BootstrapAdminUsername}}","password":"{{AppHostFixture.BootstrapAdminPassword}}"}""",
                    Encoding.UTF8,
                    "application/json"));
            if (loginResponse.IsSuccessStatusCode) break;
            await Task.Delay(1000);
        }
        loginResponse.EnsureSuccessStatusCode();
        await InitializeAntiforgeryAsync(httpClient, handler.CookieContainer);

        return (httpClient, handler);
    }

    /// <summary>
    /// Register a NATS environment pointing to the Aspire NATS container via the backend API.
    /// Returns the environment ID.
    /// </summary>
    protected async Task<string> RegisterNatsEnvironmentAsync(HttpClient httpClient, string envName)
    {
        var response = await httpClient.PostAsync("/api/environments",
            new StringContent($$"""{"name":"{{envName}}","serverUrl":"{{Fixture.NatsUrl}}","credentialType":"None"}""",
                System.Text.Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        // Extract ID from JSON response
        var doc = System.Text.Json.JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    /// <summary>
    /// Login as admin and register a NATS environment pointing to the Aspire container.
    /// Injects session cookie into the browser context and navigates to the target page.
    /// Returns the environment ID.
    /// </summary>
    protected async Task<string> LoginAndSetupEnvironmentAsync(string navigateTo = "/dashboard")
    {
        var (httpClient, handler) = await CreateAuthenticatedHttpClientAsync();
        using (httpClient)
        using (handler)
        {
            var envName = $"e2e-{Guid.NewGuid():N}"[..16];
            var envId = await RegisterNatsEnvironmentAsync(httpClient, envName);

            await CopyBackendCookiesToBrowserAsync(handler.CookieContainer);

            await NavigateAsync(navigateTo);

            // Select the environment in the sidebar
            var envSelector = Page.GetByPlaceholder("Select environment");
            await envSelector.ClickAsync();
            await Page.GetByText(envName).ClickAsync();

            return envId;
        }
    }

    protected static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    protected static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);

    protected async Task FilterEnvironmentsAsync(string searchTerm)
    {
        var searchInput = Page.GetByPlaceholder("Search environments...");
        await Expect(searchInput).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await searchInput.FillAsync(searchTerm);
    }

    private async Task InitializeAntiforgeryAsync(HttpClient httpClient, CookieContainer cookieContainer)
    {
        var meResponse = await httpClient.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var xsrfCookie = cookieContainer.GetCookies(new Uri(Fixture.BackendUrl))["XSRF-TOKEN"]
            ?? throw new Exception("No XSRF-TOKEN cookie returned from backend.");

        httpClient.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        httpClient.DefaultRequestHeaders.Add("X-XSRF-TOKEN", xsrfCookie.Value);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private async Task CopyBackendCookiesToBrowserAsync(CookieContainer cookieContainer)
    {
        var backendCookies = cookieContainer.GetCookies(new Uri(Fixture.BackendUrl));
        var frontendUri = new Uri(Fixture.FrontendUrl);
        var isFrontendSecure = string.Equals(frontendUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

        var cookies = backendCookies
            .Cast<System.Net.Cookie>()
            .Select(cookie => new Microsoft.Playwright.Cookie
            {
                Name = cookie.Name,
                Value = cookie.Value,
                Url = Fixture.FrontendUrl,
                HttpOnly = cookie.HttpOnly,
                Secure = isFrontendSecure,
                SameSite = SameSiteAttribute.Strict,
            })
            .ToArray();

        if (cookies.Length == 0)
        {
            throw new Exception("No backend cookies were available to inject into the browser context.");
        }

        await Page.Context.AddCookiesAsync(cookies);
    }
}

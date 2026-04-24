namespace NatsManager.Web.Middleware;

/// <summary>
/// Adds baseline HTTP security response headers on every request.
/// Complements HSTS (registered separately) and the antiforgery cookie.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Disallow MIME-sniffing to prevent content-type confusion attacks.
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent the UI from being embedded in iframes on other origins (clickjacking defence).
        headers["X-Frame-Options"] = "DENY";

        // Limit referrer leakage to third parties.
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Deny powerful browser features we don't use.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

        // Baseline CSP. The frontend is served as static assets and calls the same origin,
        // so 'self' is sufficient. Keep 'unsafe-inline' for styles to avoid breaking
        // Mantine's injected style tags; review once a nonce/hash-based policy is wired up.
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; " +
                "font-src 'self' data:; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";
        }

        return _next(context);
    }
}

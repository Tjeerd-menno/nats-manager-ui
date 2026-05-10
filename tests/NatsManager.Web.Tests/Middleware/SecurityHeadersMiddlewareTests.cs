using Microsoft.AspNetCore.Http;
using NatsManager.Web.Middleware;
using Shouldly;

namespace NatsManager.Web.Tests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithNoExistingSecurityHeaders_ShouldAddBaselineHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
        headers["X-Frame-Options"].ToString().ShouldBe("DENY");
        headers["Referrer-Policy"].ToString().ShouldBe("strict-origin-when-cross-origin");
        headers["Permissions-Policy"].ToString().ShouldContain("camera=()");
        headers["Content-Security-Policy"].ToString().ShouldContain("default-src 'self'");
        headers["Content-Security-Policy"].ToString().ShouldContain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task InvokeAsync_WithExistingContentSecurityPolicy_ShouldNotOverwritePolicy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers["Content-Security-Policy"] = "default-src https://example.test";
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers["Content-Security-Policy"].ToString().ShouldBe("default-src https://example.test");
        context.Response.Headers["X-Content-Type-Options"].ToString().ShouldBe("nosniff");
    }
}

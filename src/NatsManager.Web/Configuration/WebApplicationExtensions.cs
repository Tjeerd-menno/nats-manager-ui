using Microsoft.AspNetCore.Antiforgery;
using NatsManager.Web.Endpoints;
using NatsManager.Web.Hubs;
using NatsManager.Web.Middleware;
using Serilog;

namespace NatsManager.Web.Configuration;

/// <summary>
/// Composition-root extension methods for the HTTP pipeline and endpoint mapping,
/// keeping <c>Program.cs</c> focused on high-level wiring.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>Configures the middleware pipeline. Ordering is significant and mirrors
    /// the previous inline configuration exactly.</summary>
    public static WebApplication UseNatsManagerPipeline(this WebApplication app)
    {
        var crossOriginEnabled = app.Configuration.GetAllowedCorsOrigins().Length > 0;

        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        // Apply baseline security headers before any handler so every response
        // (including error responses) is covered.
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseSerilogRequestLogging();
        app.UseCors();
        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        // Enable rate limiting after authentication so that authenticated users
        // get their own partition.
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseRateLimiter();
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.UseAntiforgery();
            app.UseApiAntiforgeryTokens(crossOriginEnabled);
        }

        app.UseMiddleware<AuditContextMiddleware>();
        app.UseMiddleware<DataFreshnessMiddleware>();

        return app;
    }

    private static void UseApiAntiforgeryTokens(this WebApplication app, bool crossOriginEnabled)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

                if (!HttpMethods.IsGet(context.Request.Method)
                    && !HttpMethods.IsHead(context.Request.Method)
                    && !HttpMethods.IsOptions(context.Request.Method)
                    && !HttpMethods.IsTrace(context.Request.Method)
                    && !context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await antiforgery.ValidateRequestAsync(context);
                    }
                    catch (AntiforgeryValidationException)
                    {
                        await Results.Problem(
                            title: "Invalid antiforgery token",
                            detail: "Unsafe API requests require a valid X-XSRF-TOKEN header.",
                            statusCode: StatusCodes.Status400BadRequest)
                            .ExecuteAsync(context);
                        return;
                    }
                }
                else if (HttpMethods.IsGet(context.Request.Method))
                {
                    var tokens = antiforgery.GetAndStoreTokens(context);

                    if (!string.IsNullOrEmpty(tokens.RequestToken))
                    {
                        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken, new CookieOptions
                        {
                            HttpOnly = false,
                            IsEssential = true,
                            SameSite = crossOriginEnabled ? SameSiteMode.None : SameSiteMode.Strict,
                            Secure = crossOriginEnabled || context.Request.IsHttps
                        });
                    }
                }
            }

            await next(context);
        });
    }

    /// <summary>Maps SignalR hubs and all module API endpoints.</summary>
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapPermissionManifestEndpoints();

        // Map SignalR hubs
        app.MapHub<MonitoringHub>("/hubs/monitoring");

        // Map API endpoints
        app.MapEnvironmentEndpoints();
        app.MapJetStreamEndpoints();
        app.MapKvEndpoints();
        app.MapDashboardEndpoints();
        app.MapServiceEndpoints();
        app.MapObjectStoreEndpoints();
        app.MapCoreNatsEndpoints();
        app.MapAuthEndpoints();
        app.MapAccessControlEndpoints();
        app.MapAuditEndpoints();
        app.MapSearchEndpoints();
        app.MapMonitoringEndpoints();
        app.MapRelationshipMapEndpoints();

        return app;
    }
}

namespace NatsManager.Web.Middleware;

public sealed class DataFreshnessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only add freshness headers for NATS-sourced API responses
        if (!context.Response.HasStarted
            && context.Request.Path.StartsWithSegments("/api")
            && context.Response.StatusCode is >= 200 and < 300)
        {
            if (!context.Response.Headers.ContainsKey("X-Data-Freshness"))
            {
                context.Response.Headers["X-Data-Freshness"] = "live";
                context.Response.Headers["X-Data-Timestamp"] = DateTimeOffset.UtcNow.ToString("o");
            }
        }
    }
}

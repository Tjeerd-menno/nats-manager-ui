namespace NatsManager.Web.Middleware;

public sealed class DataFreshnessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Register the callback before invoking the rest of the pipeline so the headers are written
        // just before the response starts flushing. Adding them after `next` fails for endpoints that
        // begin writing the body themselves (HasStarted would already be true).
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;

            // Only add freshness headers for successful NATS-sourced API responses.
            if (ctx.Request.Path.StartsWithSegments("/api")
                && ctx.Response.StatusCode is >= 200 and < 300
                && !ctx.Response.Headers.ContainsKey("X-Data-Freshness"))
            {
                ctx.Response.Headers["X-Data-Freshness"] = "live";
                ctx.Response.Headers["X-Data-Timestamp"] = DateTimeOffset.UtcNow.ToString("o");
            }

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}

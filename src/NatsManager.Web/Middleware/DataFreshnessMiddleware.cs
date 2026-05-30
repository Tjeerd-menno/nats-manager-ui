namespace NatsManager.Web.Middleware;

public sealed class DataFreshnessMiddleware(RequestDelegate next)
{
    private const string ApiPathPrefix = "/api";
    private const string FreshnessHeader = "X-Data-Freshness";
    private const string TimestampHeader = "X-Data-Timestamp";
    private const string LiveFreshnessValue = "live";

    public async Task InvokeAsync(HttpContext context)
    {
        // Register the callback before invoking the rest of the pipeline so the headers are written
        // just before the response starts flushing. Adding them after `next` fails for endpoints that
        // begin writing the body themselves (HasStarted would already be true).
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;

            // Only add freshness headers for successful NATS-sourced API responses.
            if (ctx.Request.Path.StartsWithSegments(ApiPathPrefix)
                && ctx.Response.StatusCode is >= 200 and < 300
                && !ctx.Response.Headers.ContainsKey(FreshnessHeader))
            {
                ctx.Response.Headers[FreshnessHeader] = LiveFreshnessValue;
                ctx.Response.Headers[TimestampHeader] = DateTimeOffset.UtcNow.ToString("o");
            }

            return Task.CompletedTask;
        }, context);

        await next(context);
    }
}

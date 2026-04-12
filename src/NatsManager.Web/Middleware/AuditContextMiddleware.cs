using System.Security.Claims;
using NatsManager.Application.Behaviors;

namespace NatsManager.Web.Middleware;

public sealed class AuditContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, HttpAuditContext auditContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                auditContext.ActorId = userId;
            }

            auditContext.ActorName = context.User.FindFirst("DisplayName")?.Value
                ?? context.User.Identity.Name
                ?? "Unknown";
        }

        await next(context);
    }
}

public sealed class HttpAuditContext : IAuditContext
{
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = "System";
}

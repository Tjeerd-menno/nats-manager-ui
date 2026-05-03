using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Web.Security;

public static class ScopedRoleClaims
{
    public const string ClaimType = "natsmanager:scoped-role";

    private const char Separator = '|';

    public static Claim Create(string role, Guid environmentId) =>
        new(ClaimType, $"{role}{Separator}{environmentId:D}");

    public static bool IsInRoleForEnvironment(this ClaimsPrincipal user, string role, Guid? environmentId)
    {
        if (user.IsInRole(role))
        {
            return true;
        }

        if (!environmentId.HasValue)
        {
            return false;
        }

        return user.FindAll(ClaimType).Any(claim =>
            TryRead(claim, out var scopedRole, out var scopedEnvironmentId)
            && string.Equals(scopedRole, role, StringComparison.Ordinal)
            && scopedEnvironmentId == environmentId.Value);
    }

    public static bool TryRead(Claim claim, out string role, out Guid environmentId)
    {
        role = string.Empty;
        environmentId = Guid.Empty;

        if (!string.Equals(claim.Type, ClaimType, StringComparison.Ordinal))
        {
            return false;
        }

        var separatorIndex = claim.Value.LastIndexOf(Separator);
        if (separatorIndex <= 0 || separatorIndex == claim.Value.Length - 1)
        {
            return false;
        }

        role = claim.Value[..separatorIndex];
        return Guid.TryParse(claim.Value[(separatorIndex + 1)..], out environmentId);
    }
}

public sealed class EnvironmentScopedRoleRequirement(params string[] roles) : IAuthorizationRequirement
{
    public IReadOnlySet<string> Roles { get; } = roles.ToHashSet(StringComparer.Ordinal);
}

public sealed class EnvironmentScopedRoleAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<EnvironmentScopedRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        EnvironmentScopedRoleRequirement requirement)
    {
        var environmentId = GetEnvironmentId(httpContextAccessor.HttpContext);

        if (requirement.Roles.Any(role => context.User.IsInRoleForEnvironment(role, environmentId)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static Guid? GetEnvironmentId(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        foreach (var routeKey in new[] { "envId", "id", "environmentId" })
        {
            if (httpContext.Request.RouteValues.TryGetValue(routeKey, out var value)
                && Guid.TryParse(value?.ToString(), out var environmentId))
            {
                return environmentId;
            }
        }

        return null;
    }
}

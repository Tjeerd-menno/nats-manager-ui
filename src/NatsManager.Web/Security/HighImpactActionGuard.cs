using System.Security.Claims;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Web.Security;

public static class HighImpactActionGuard
{
    public static async Task<IResult?> RequireAllowedAsync(
        Guid environmentId,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, cancellationToken);
        if (environment is not { IsProduction: true })
        {
            return null;
        }

        if (user.IsInRoleForEnvironment(Role.PredefinedNames.Administrator, environmentId))
        {
            return null;
        }

        return Results.Problem(
            title: "Production safeguard blocked this action",
            detail: "High-impact actions in production environments require administrator permissions.",
            statusCode: StatusCodes.Status403Forbidden);
    }
}

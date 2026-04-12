using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Modules.Auth.Services;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Persistence;

namespace NatsManager.Infrastructure.Auth;

public sealed class AuthorizationService(AppDbContext context) : IAuthorizationService
{
    private static readonly Dictionary<string, int> RoleHierarchy = new()
    {
        [Role.PredefinedNames.ReadOnly] = 0,
        [Role.PredefinedNames.Auditor] = 1,
        [Role.PredefinedNames.Operator] = 2,
        [Role.PredefinedNames.Administrator] = 3
    };

    public async Task<bool> CanPerformActionAsync(
        Guid userId,
        string requiredRole,
        Guid? environmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!RoleHierarchy.TryGetValue(requiredRole, out var requiredLevel))
        {
            return false;
        }

        var assignments = await context.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => a.EnvironmentId == null || a.EnvironmentId == environmentId)
            .Join(context.Roles, a => a.RoleId, r => r.Id, (a, r) => new { r.Name, a.EnvironmentId })
            .ToListAsync(cancellationToken);

        // Check environment-specific role first, fall back to global
        var effectiveRole = environmentId.HasValue
            ? assignments.FirstOrDefault(a => a.EnvironmentId == environmentId)?.Name
              ?? assignments.FirstOrDefault(a => a.EnvironmentId == null)?.Name
            : assignments.FirstOrDefault(a => a.EnvironmentId == null)?.Name;

        if (effectiveRole is null || !RoleHierarchy.TryGetValue(effectiveRole, out var userLevel))
        {
            return false;
        }

        return userLevel >= requiredLevel;
    }

    public async Task<bool> IsProductionRestricted(
        Guid userId,
        Guid environmentId,
        CancellationToken cancellationToken = default)
    {
        var isProduction = await context.Set<Domain.Modules.Environments.Environment>()
            .AsNoTracking()
            .Where(e => e.Id == environmentId)
            .Select(e => e.IsProduction)
            .FirstOrDefaultAsync(cancellationToken);

        if (!isProduction)
        {
            return false;
        }

        return !await CanPerformActionAsync(userId, Role.PredefinedNames.Administrator, environmentId, cancellationToken);
    }
}

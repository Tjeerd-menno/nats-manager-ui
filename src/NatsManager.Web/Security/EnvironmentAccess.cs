using System.Security.Claims;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Web.Security;

/// <summary>
/// Canonical definition of which roles grant read access to an environment's resources.
/// Used as the single source of truth for both the <c>EnvironmentReadAccess</c> authorization
/// policy (HTTP endpoints) and the explicit in-method check in <c>MonitoringHub</c> (SignalR),
/// where a route-value-based policy handler cannot see the environment id.
/// </summary>
public static class EnvironmentAccess
{
    /// <summary>
    /// Any user holding one of these roles for an environment (globally or env-scoped) may read it.
    /// A user who is merely authenticated but has no role for the environment is denied.
    /// </summary>
    public static readonly string[] ReadRoles =
    [
        Role.PredefinedNames.ReadOnly,
        Role.PredefinedNames.Operator,
        Role.PredefinedNames.Administrator,
        Role.PredefinedNames.Auditor,
    ];

    /// <summary>
    /// Returns true if the user may read the given environment's resources. Note that, for SignalR,
    /// this is evaluated at subscription time only; revoking a role does not evict an already-joined
    /// connection until it reconnects.
    /// </summary>
    public static bool CanRead(ClaimsPrincipal user, Guid environmentId) =>
        Array.Exists(ReadRoles, role => user.IsInRoleForEnvironment(role, environmentId));
}

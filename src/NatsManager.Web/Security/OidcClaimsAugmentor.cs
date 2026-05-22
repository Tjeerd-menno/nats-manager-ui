using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Web.Configuration;

namespace NatsManager.Web.Security;

public static class OidcClaimsAugmentor
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.Ordinal)
    {
        Role.PredefinedNames.Administrator,
        Role.PredefinedNames.Auditor,
        Role.PredefinedNames.Operator,
        Role.PredefinedNames.ReadOnly
    };

    public static void Apply(ClaimsPrincipal principal, OidcOptions options)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        AddIfMissing(identity, ClaimTypes.NameIdentifier, FindFirstValue(principal, ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub, "sub"));
        AddIfMissing(identity, ClaimTypes.Name, FindFirstValue(principal, ClaimTypes.Name, JwtRegisteredClaimNames.Name, "name", "preferred_username", ClaimTypes.Email, JwtRegisteredClaimNames.Email));
        AddIfMissing(identity, "DisplayName", FindFirstValue(principal, JwtRegisteredClaimNames.Name, "name", "preferred_username", ClaimTypes.Email, JwtRegisteredClaimNames.Email));

        var mappedRoles = principal.Claims
            .Where(static claim => claim.Type is ClaimTypes.Role or "role" or "roles")
            .SelectMany(static claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(role => options.RoleMappings.TryGetValue(role, out var mappedRole) ? mappedRole : role)
            .Where(static role => ValidRoles.Contains(role))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (mappedRoles.Count == 0)
        {
            mappedRoles.AddRange(options.DefaultRoles.Where(static role => ValidRoles.Contains(role)));
        }

        foreach (var role in mappedRoles.Distinct(StringComparer.Ordinal))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private static void AddIfMissing(ClaimsIdentity identity, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !identity.HasClaim(claim => claim.Type == claimType))
        {
            identity.AddClaim(new Claim(claimType, value));
        }
    }

    private static string? FindFirstValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

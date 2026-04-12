using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NatsManager.Infrastructure.Persistence;

namespace NatsManager.Web.Middleware;

public sealed class SessionAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    AppDbContext context) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    public const string SchemeName = "Session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var sessionUserId = Context.Session.GetString("UserId");
        if (string.IsNullOrEmpty(sessionUserId) || !Guid.TryParse(sessionUserId, out var userId))
        {
            return AuthenticateResult.NoResult();
        }

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user is null)
        {
            return AuthenticateResult.Fail("User not found or inactive");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("DisplayName", user.DisplayName)
        };

        // Load roles
        var roles = await context.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Join(context.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name)
            .ToListAsync();

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}

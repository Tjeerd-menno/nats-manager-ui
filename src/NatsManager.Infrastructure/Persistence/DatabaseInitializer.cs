using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Auth;
using NatsManager.Infrastructure.Configuration;

namespace NatsManager.Infrastructure.Persistence;

public sealed partial class DatabaseInitializer(
    AppDbContext context,
    IOptions<BootstrapAdminOptions> bootstrapAdminOptions,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);

        // Enable WAL mode for SQLite
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);

        await SeedRolesAsync(cancellationToken);
        await SeedDefaultAdminAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (await context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var roles = new[]
        {
            Role.Create(Role.PredefinedNames.ReadOnly, "View all resources, no state-changing actions"),
            Role.Create(Role.PredefinedNames.Operator, "View and modify resources, destructive actions blocked in production"),
            Role.Create(Role.PredefinedNames.Administrator, "Full access including destructive actions and user management"),
            Role.Create(Role.PredefinedNames.Auditor, "View resources and audit history, no modifications")
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync(cancellationToken);
        LogRolesSeeded(roles.Length);
    }

    private async Task SeedDefaultAdminAsync(CancellationToken cancellationToken)
    {
        var bootstrapAdmin = bootstrapAdminOptions.Value;
        var hasUsers = await context.Users.AnyAsync(cancellationToken);
        var usernameMissing = string.IsNullOrWhiteSpace(bootstrapAdmin.Username);
        var passwordMissing = string.IsNullOrWhiteSpace(bootstrapAdmin.Password);

        if (usernameMissing && passwordMissing)
        {
            if (hasUsers)
            {
                return;
            }

            throw new InvalidOperationException(
                "BootstrapAdmin:Username and BootstrapAdmin:Password must be configured when initializing an empty database.");
        }

        if (usernameMissing || passwordMissing)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Username and BootstrapAdmin:Password must both be configured when bootstrap admin synchronization is enabled.");
        }

        var bootstrapUsername = bootstrapAdmin.Username!;
        var bootstrapPassword = bootstrapAdmin.Password!;
        if (bootstrapPassword.Length < 8)
        {
            throw new InvalidOperationException("BootstrapAdmin:Password must be at least 8 characters long.");
        }

        var adminRole = await context.Roles.FirstAsync(r => r.Name == Role.PredefinedNames.Administrator, cancellationToken);
        var admin = await context.Users.SingleOrDefaultAsync(
            user => user.Username == bootstrapUsername,
            cancellationToken);

        if (admin is null)
        {
            var hashedPassword = PasswordHasher.Hash(bootstrapPassword);
            admin = User.Create(bootstrapUsername, bootstrapAdmin.DisplayName, hashedPassword);
            context.Users.Add(admin);

            var assignment = UserRoleAssignment.Create(admin.Id, adminRole.Id, environmentId: null, assignedBy: admin.Id);
            context.UserRoleAssignments.Add(assignment);

            await context.SaveChangesAsync(cancellationToken);
            LogBootstrapAdminSynchronized(admin.Username, hasUsers ? "created" : "seeded");
            return;
        }

        var changed = false;
        if (!string.Equals(admin.DisplayName, bootstrapAdmin.DisplayName, StringComparison.Ordinal))
        {
            admin.UpdateProfile(bootstrapAdmin.DisplayName);
            changed = true;
        }

        if (!PasswordHasher.Verify(bootstrapPassword, admin.PasswordHash))
        {
            admin.UpdatePassword(PasswordHasher.Hash(bootstrapPassword));
            changed = true;
        }

        if (!admin.IsActive)
        {
            admin.Activate();
            changed = true;
        }

        var hasAdminAssignment = await context.UserRoleAssignments.AnyAsync(
            assignment => assignment.UserId == admin.Id && assignment.RoleId == adminRole.Id && assignment.EnvironmentId == null,
            cancellationToken);

        if (!hasAdminAssignment)
        {
            context.UserRoleAssignments.Add(UserRoleAssignment.Create(admin.Id, adminRole.Id, environmentId: null, assignedBy: admin.Id));
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        LogBootstrapAdminSynchronized(admin.Username, "updated");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} predefined roles")]
    private partial void LogRolesSeeded(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Synchronized bootstrap admin user '{Username}' ({Action})")]
    private partial void LogBootstrapAdminSynchronized(string username, string action);
}

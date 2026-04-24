using Shouldly;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Auth;
using NatsManager.Infrastructure.Configuration;
using NatsManager.Infrastructure.Persistence;

namespace NatsManager.Infrastructure.Tests.Persistence;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_WhenDatabaseIsEmptyAndBootstrapCredentialsMissing_ShouldThrow()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = CreateContext(connection);
        var initializer = CreateInitializer(context, new BootstrapAdminOptions());

        var act = () => initializer.InitializeAsync();

        (await Should.ThrowAsync<InvalidOperationException>(act)).Message.ShouldContain("BootstrapAdmin:Username and BootstrapAdmin:Password");
    }

    [Fact]
    public async Task InitializeAsync_WhenBootstrapCredentialsAreConfigured_ShouldSeedAdministrator()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var context = CreateContext(connection);
        var initializer = CreateInitializer(context, new BootstrapAdminOptions
        {
            Username = "bootstrap-admin",
            Password = "Bootstrap123!",
            DisplayName = "Bootstrap Admin"
        });

        await initializer.InitializeAsync();

        var admin = await context.Users.SingleAsync();
        admin.Username.ShouldBe("bootstrap-admin");
        admin.DisplayName.ShouldBe("Bootstrap Admin");

        var adminRole = await context.Roles.SingleAsync(role => role.Name == Role.PredefinedNames.Administrator);
        var assignment = await context.UserRoleAssignments.SingleAsync();
        assignment.UserId.ShouldBe(admin.Id);
        assignment.RoleId.ShouldBe(adminRole.Id);
    }

    [Fact]
    public async Task InitializeAsync_WhenUsersAlreadyExist_ShouldNotRequireBootstrapCredentials()
    {
        await using var connection = await CreateOpenConnectionAsync();

        await using (var initialContext = CreateContext(connection))
        {
            var initialInitializer = CreateInitializer(initialContext, new BootstrapAdminOptions
            {
                Username = "bootstrap-admin",
                Password = "Bootstrap123!"
            });

            await initialInitializer.InitializeAsync();
        }

        await using (var existingContext = CreateContext(connection))
        {
            var initializer = CreateInitializer(existingContext, new BootstrapAdminOptions());

            var act = () => initializer.InitializeAsync();

            await Should.NotThrowAsync(act);
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenBootstrapAdminAlreadyExists_ShouldUpdatePasswordToConfiguredValue()
    {
        await using var connection = await CreateOpenConnectionAsync();

        await using (var initialContext = CreateContext(connection))
        {
            var initialInitializer = CreateInitializer(initialContext, new BootstrapAdminOptions
            {
                Username = "admin",
                Password = "Original123!",
                DisplayName = "Administrator"
            });

            await initialInitializer.InitializeAsync();
        }

        await using (var updatedContext = CreateContext(connection))
        {
            var initializer = CreateInitializer(updatedContext, new BootstrapAdminOptions
            {
                Username = "admin",
                Password = "Updated123!",
                DisplayName = "Administrator"
            });

            await initializer.InitializeAsync();
        }

        await using (var assertionContext = CreateContext(connection))
        {
            var admin = await assertionContext.Users.SingleAsync();
            PasswordHasher.Verify("Updated123!", admin.PasswordHash).ShouldBeTrue();
            PasswordHasher.Verify("Original123!", admin.PasswordHash).ShouldBeFalse();

            var adminRole = await assertionContext.Roles.SingleAsync(role => role.Name == Role.PredefinedNames.Administrator);
            var assignments = await assertionContext.UserRoleAssignments
                .Where(assignment => assignment.UserId == admin.Id && assignment.RoleId == adminRole.Id)
                .ToListAsync();

            assignments.Count().ShouldBe(1);
        }
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static AppDbContext CreateContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);

    private static DatabaseInitializer CreateInitializer(AppDbContext context, BootstrapAdminOptions options)
        => new(context, Options.Create(options), NullLogger<DatabaseInitializer>.Instance);
}

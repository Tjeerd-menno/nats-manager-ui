using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Infrastructure.Auth;
using NatsManager.Infrastructure.Configuration;
using NatsManager.Infrastructure.Persistence;
using Shouldly;
using Testcontainers.PostgreSql;

namespace NatsManager.Infrastructure.Tests.Persistence;

/// <summary>
/// Integration tests that exercise <see cref="DatabaseInitializer"/> against a real PostgreSQL
/// instance. These complement the SQLite-backed unit tests in
/// <see cref="DatabaseInitializerTests"/> and protect against provider-specific regressions:
/// the PostgreSQL migration set applies cleanly, role seeding runs, the bootstrap admin is
/// created, and the SQLite-only <c>PRAGMA journal_mode=WAL</c> call is correctly skipped.
/// <para>
/// The tests require Docker. When Docker is unavailable they are silently skipped via
/// <see cref="DockerAvailable"/> so that the default <c>dotnet test</c> command continues to work
/// on developer machines and CI runners without container support.
/// </para>
/// </summary>
public sealed class DatabaseInitializerPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _container = DockerAvailable
        ? new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithDatabase("natsmanager")
            .WithUsername("test")
            .WithPassword("test")
            .Build()
        : null;

    private static bool DockerAvailable
        => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST"))
           || File.Exists("/var/run/docker.sock")
           || File.Exists(@"\\.\pipe\docker_engine");

    public async ValueTask InitializeAsync()
    {
        if (_container is not null)
        {
            await _container.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task InitializeAsync_AppliesPostgresMigrationsAndSeedsBootstrapAdmin()
    {
        if (_container is null)
        {
            // Docker not available — silently treat as inconclusive.
            return;
        }

        await using var context = CreatePostgresContext(_container.GetConnectionString());
        var initializer = new DatabaseInitializer(
            context,
            Options.Create(new BootstrapAdminOptions
            {
                Username = "bootstrap-admin",
                Password = "Bootstrap123!",
                DisplayName = "Bootstrap Admin"
            }),
            NullLogger<DatabaseInitializer>.Instance);

        await initializer.InitializeAsync();

        // The migrations applied — every aggregate table is queryable.
        var admin = await context.Users.SingleAsync();
        admin.Username.ShouldBe("bootstrap-admin");
        admin.DisplayName.ShouldBe("Bootstrap Admin");

        var adminRole = await context.Roles.SingleAsync(r => r.Name == Role.PredefinedNames.Administrator);
        var assignment = await context.UserRoleAssignments.SingleAsync();
        assignment.UserId.ShouldBe(admin.Id);
        assignment.RoleId.ShouldBe(adminRole.Id);

        // The provider scoping discovered the Postgres migrations only.
        var providerName = context.Database.ProviderName ?? string.Empty;
        providerName.ShouldContain("Npgsql", Case.Insensitive);
        var applied = await context.Database.GetAppliedMigrationsAsync();
        applied.ShouldContain(m => m.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializeAsync_RunningTwice_IsIdempotentOnPostgres()
    {
        if (_container is null)
        {
            return;
        }

        var bootstrap = new BootstrapAdminOptions
        {
            Username = "bootstrap-admin",
            Password = "Bootstrap123!",
            DisplayName = "Bootstrap Admin"
        };

        await using (var context = CreatePostgresContext(_container.GetConnectionString()))
        {
            var initializer = new DatabaseInitializer(context, Options.Create(bootstrap), NullLogger<DatabaseInitializer>.Instance);
            await initializer.InitializeAsync();
        }

        await using (var context = CreatePostgresContext(_container.GetConnectionString()))
        {
            var initializer = new DatabaseInitializer(context, Options.Create(bootstrap), NullLogger<DatabaseInitializer>.Instance);
            await initializer.InitializeAsync();

            (await context.Users.CountAsync()).ShouldBe(1);
            (await context.UserRoleAssignments.CountAsync()).ShouldBe(1);
        }
    }

    private static AppDbContext CreatePostgresContext(string connectionString)
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options);
}

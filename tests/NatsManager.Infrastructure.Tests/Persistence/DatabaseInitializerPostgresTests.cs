using DotNet.Testcontainers.Containers;
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
/// The tests require a working Docker daemon. Rather than guess at availability via
/// environment heuristics (which can yield false positives when <c>DOCKER_HOST</c> is set but
/// unreachable, or false negatives when Docker is reachable through non-default contexts),
/// we attempt to actually start the Postgres container in <see cref="InitializeAsync"/>. If
/// that fails, every test calls <see cref="Assert.Skip(string)"/> so the missing integration
/// coverage is reported as a real skip rather than a silent pass.
/// </para>
/// </summary>
public sealed class DatabaseInitializerPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("natsmanager")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private string? _skipReason;

    public async ValueTask InitializeAsync()
    {
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex) when (ex is FileNotFoundException
            // Testcontainers wraps the underlying Docker.DotNet failures in these types, but we
            // also need a broad catch because the exact exception type varies across platforms
            // (TimeoutException, HttpRequestException, IOException, etc.) when the daemon is
            // missing or unreachable. Anything that prevents a successful start translates to a
            // skip — a healthy Docker setup will never throw here.
            || ex.GetType().Namespace?.StartsWith("Docker.", StringComparison.Ordinal) == true
            || ex is TimeoutException
            || ex is HttpRequestException
            || ex is IOException
            || ex is InvalidOperationException
            || ex is ResourceReaperException)
        {
            _skipReason = $"Docker is not available ({ex.GetType().Name}: {ex.Message}); skipping PostgreSQL integration tests.";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_AppliesPostgresMigrationsAndSeedsBootstrapAdmin()
    {
        Assert.SkipWhen(_skipReason is not null, _skipReason ?? string.Empty);

        await using var context = CreatePostgresContext(_container.GetConnectionString());
        var initializer = new DatabaseInitializer(
            context,
            Options.Create(new BootstrapAdminOptions
            {
                Username = "bootstrap-admin",
                Password = "Bootstrap123!",
                DisplayName = "Bootstrap Admin"
            }),
            new PasswordHasher(),
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
        Assert.SkipWhen(_skipReason is not null, _skipReason ?? string.Empty);

        var bootstrap = new BootstrapAdminOptions
        {
            Username = "bootstrap-admin",
            Password = "Bootstrap123!",
            DisplayName = "Bootstrap Admin"
        };

        await using (var context = CreatePostgresContext(_container.GetConnectionString()))
        {
            var initializer = new DatabaseInitializer(context, Options.Create(bootstrap), new PasswordHasher(), NullLogger<DatabaseInitializer>.Instance);
            await initializer.InitializeAsync();
        }

        await using (var context = CreatePostgresContext(_container.GetConnectionString()))
        {
            var initializer = new DatabaseInitializer(context, Options.Create(bootstrap), new PasswordHasher(), NullLogger<DatabaseInitializer>.Instance);
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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NatsManager.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> tooling to generate / apply migrations for either
/// SQLite or PostgreSQL.
/// <para>
/// Provider selection (in order of precedence):
/// <list type="number">
///   <item><c>--provider Sqlite|Postgres</c> on the EF CLI (after <c>--</c>)</item>
///   <item>The <c>DESIGNTIME_PROVIDER</c> environment variable</item>
///   <item>Default: <c>Sqlite</c></item>
/// </list>
/// The connection string can be overridden with <c>DESIGNTIME_CONNECTION_STRING</c>; otherwise a
/// throwaway local default is used (suitable for offline scaffolding — no real database
/// connection is required to generate migrations).
/// </para>
/// <example>
/// Generate a SQLite migration:
/// <code>dotnet ef migrations add MyChange --output-dir Persistence/Migrations/Sqlite --namespace NatsManager.Infrastructure.Persistence.Migrations.Sqlite</code>
/// Generate a PostgreSQL migration:
/// <code>dotnet ef migrations add MyChange --output-dir Persistence/Migrations/Postgres --namespace NatsManager.Infrastructure.Persistence.Migrations.Postgres -- --provider Postgres</code>
/// </example>
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultSqliteConnectionString = "Data Source=natsmanager.db";
    private const string DefaultPostgresConnectionString =
        "Host=localhost;Port=5432;Database=natsmanager;Username=postgres;Password=postgres";

    public AppDbContext CreateDbContext(string[] args)
    {
        var provider = ResolveProvider(args);
        var connectionString = Environment.GetEnvironmentVariable("DESIGNTIME_CONNECTION_STRING");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(connectionString ?? DefaultPostgresConnectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString ?? DefaultSqliteConnectionString);
        }

        // Note: the ProviderScopedMigrationsAssembly is wired up inside AppDbContext.OnConfiguring,
        // so we don't need to call ReplaceService here.
        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveProvider(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--provider", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[i], "-p", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        var envProvider = Environment.GetEnvironmentVariable("DESIGNTIME_PROVIDER");
        return string.IsNullOrWhiteSpace(envProvider) ? "Sqlite" : envProvider;
    }
}

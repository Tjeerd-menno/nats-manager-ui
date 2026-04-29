namespace NatsManager.Infrastructure.Configuration;

/// <summary>
/// Selects the database provider used by <see cref="NatsManager.Infrastructure.Persistence.AppDbContext"/>.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>SQLite (default; zero-config local development).</summary>
    Sqlite = 0,

    /// <summary>PostgreSQL (opt-in for production / shared deployments).</summary>
    Postgres = 1
}

/// <summary>
/// Strongly-typed options for the application database, bound to the <c>Database</c> configuration section.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Provider selected at runtime. Defaults to <see cref="DatabaseProvider.Sqlite"/> so existing
    /// deployments and local development continue to work with no configuration.
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
}

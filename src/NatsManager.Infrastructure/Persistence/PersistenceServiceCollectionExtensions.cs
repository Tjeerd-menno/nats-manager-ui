using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NatsManager.Infrastructure.Configuration;

namespace NatsManager.Infrastructure.Persistence;

/// <summary>
/// Composition root extensions for the application's persistence layer.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    private const string DefaultSqliteConnectionString = "Data Source=natsmanager.db";

    /// <summary>
    /// Registers <see cref="AppDbContext"/> with the EF Core provider selected by
    /// <c>Database:Provider</c> (default: <see cref="DatabaseProvider.Sqlite"/>) and the
    /// connection string from <c>ConnectionStrings:DefaultConnection</c>.
    /// </summary>
    public static IServiceCollection AddNatsManagerPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        switch (databaseOptions.Provider)
        {
            case DatabaseProvider.Postgres:
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:DefaultConnection must be configured when Database:Provider is 'Postgres'.");
                }
                services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
                break;

            case DatabaseProvider.Sqlite:
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(connectionString ?? DefaultSqliteConnectionString));
                break;

            default:
                throw new InvalidOperationException(
                    $"Database:Provider value '{databaseOptions.Provider}' is not supported. Use 'Sqlite' or 'Postgres'.");
        }

        services.AddSingleton(databaseOptions);
        return services;
    }
}

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace NatsManager.Infrastructure.Persistence;

/// <summary>
/// A custom <see cref="IMigrationsAssembly"/> that scopes discovered migrations and the model
/// snapshot to a single provider-specific sub-namespace inside the Infrastructure assembly.
/// <para>
/// This lets us host multiple provider migration sets (one per <c>Database:Provider</c>) in the
/// same assembly without EF complaining about ambiguous migration IDs or model snapshots:
/// SQLite migrations live under <c>...Persistence.Migrations.Sqlite</c> and PostgreSQL migrations
/// under <c>...Persistence.Migrations.Postgres</c>.
/// </para>
/// </summary>
#pragma warning disable EF1001 // Internal EF Core API usage — required to override migration discovery.
internal sealed class ProviderScopedMigrationsAssembly : MigrationsAssembly
{
    private readonly ICurrentDbContext _currentContext;

    public ProviderScopedMigrationsAssembly(
        ICurrentDbContext currentContext,
        IDbContextOptions options,
        IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
        : base(currentContext, options, idGenerator, logger)
    {
        _currentContext = currentContext;
    }

    private const string MigrationsRootNamespace = "NatsManager.Infrastructure.Persistence.Migrations";

    private string ProviderSegment
    {
        get
        {
            var providerName = _currentContext.Context.Database.ProviderName ?? string.Empty;
            // Microsoft.EntityFrameworkCore.Sqlite / Npgsql.EntityFrameworkCore.PostgreSQL
            if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return "Sqlite";
            }
            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || providerName.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                return "Postgres";
            }
            // Fallback: don't filter (e.g. the in-memory provider used by some tests).
            return string.Empty;
        }
    }

    public override IReadOnlyDictionary<string, TypeInfo> Migrations
    {
        get
        {
            var segment = ProviderSegment;
            if (segment.Length == 0)
            {
                return base.Migrations;
            }

            var expectedNamespace = $"{MigrationsRootNamespace}.{segment}";
            return base.Migrations
                .Where(kvp => string.Equals(kvp.Value.Namespace, expectedNamespace, StringComparison.Ordinal))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);
        }
    }

    public override ModelSnapshot? ModelSnapshot
    {
        get
        {
            var segment = ProviderSegment;
            if (segment.Length == 0)
            {
                return base.ModelSnapshot;
            }

            var expectedNamespace = $"{MigrationsRootNamespace}.{segment}";
            var contextType = _currentContext.Context.GetType();

            var snapshotType = Assembly
                .DefinedTypes
                .FirstOrDefault(t =>
                    string.Equals(t.Namespace, expectedNamespace, StringComparison.Ordinal)
                    && typeof(ModelSnapshot).IsAssignableFrom(t)
                    && t.GetCustomAttribute<DbContextAttribute>()?.ContextType == contextType);

            return snapshotType is null
                ? null
                : (ModelSnapshot?)Activator.CreateInstance(snapshotType.AsType());
        }
    }
}
#pragma warning restore EF1001

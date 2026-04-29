using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Environment> Environments => Set<Environment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Always scope migrations to the active provider's sub-namespace so that the SQLite and
        // PostgreSQL migration sets (and their respective ModelSnapshots) coexist in this single
        // assembly without colliding. This is registered here — rather than in every call site —
        // so that it applies uniformly to production DI, tests, and EF design-time tooling.
        optionsBuilder.ReplaceService<IMigrationsAssembly, ProviderScopedMigrationsAssembly>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

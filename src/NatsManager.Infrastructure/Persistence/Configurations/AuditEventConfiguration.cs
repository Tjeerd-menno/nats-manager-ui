using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NatsManager.Domain.Modules.Audit;

namespace NatsManager.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(a => a.Id);

        // Timestamp uses the EF default mapping (no value converter), matching every other
        // DateTimeOffset property in the model. This produces:
        //   - PostgreSQL: `timestamp with time zone` (8-byte timestamptz, the canonical
        //     Npgsql / PostgreSQL best practice for instant data — efficient indexes, native
        //     range comparisons, low storage).
        //   - SQLite: TEXT in EF's standard ISO 8601 round-trip format
        //     (`yyyy-MM-dd HH:mm:ss.FFFFFFFzzz`), which sorts correctly lexicographically.
        // All values originate from `DateTimeOffset.UtcNow`, so no offset information is lost
        // by either provider's storage shape.
        builder.Property(a => a.ActorName).HasMaxLength(200);
        builder.Property(a => a.ActionType).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.ResourceType).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.ResourceId).HasMaxLength(500);
        builder.Property(a => a.ResourceName).HasMaxLength(500);
        builder.Property(a => a.Outcome).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.Source).HasConversion<string>().HasMaxLength(50);

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.ActorId);
        builder.HasIndex(a => a.ActionType);
        builder.HasIndex(a => a.ResourceType);
    }
}

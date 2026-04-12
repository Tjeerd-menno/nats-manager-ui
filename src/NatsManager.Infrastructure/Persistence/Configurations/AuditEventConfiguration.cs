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

        builder.Property(a => a.Timestamp).HasConversion<string>();
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

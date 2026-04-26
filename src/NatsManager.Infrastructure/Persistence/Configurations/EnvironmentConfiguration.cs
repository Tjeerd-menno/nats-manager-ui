using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Persistence.Configurations;

internal sealed class EnvironmentConfiguration : IEntityTypeConfiguration<Environment>
{
    public void Configure(EntityTypeBuilder<Environment> builder)
    {
        builder.ToTable("Environments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ServerUrl).HasMaxLength(2048).IsRequired();
        builder.Property(e => e.CredentialType).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.CredentialReference).HasMaxLength(500);
        builder.Property(e => e.ConnectionStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.MonitoringUrl).HasMaxLength(500);
        builder.Property(e => e.MonitoringPollingIntervalSeconds);
    }
}

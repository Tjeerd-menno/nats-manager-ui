using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Value).HasMaxLength(2000);

        builder.HasOne<Domain.Modules.Auth.User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.UserId, p.Key }).IsUnique();
    }
}

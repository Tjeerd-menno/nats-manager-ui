using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Infrastructure.Persistence.Configurations;

internal sealed class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ResourceType).HasConversion<string>().HasMaxLength(50);
        builder.Property(b => b.ResourceId).HasMaxLength(500).IsRequired();
        builder.Property(b => b.DisplayName).HasMaxLength(200).IsRequired();

        builder.HasOne<Domain.Modules.Auth.User>().WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Domain.Modules.Environments.Environment>().WithMany().HasForeignKey(b => b.EnvironmentId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.UserId, b.EnvironmentId, b.ResourceType, b.ResourceId }).IsUnique();
    }
}

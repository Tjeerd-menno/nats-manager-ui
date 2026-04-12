using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("UserRoleAssignments");
        builder.HasKey(a => a.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>().WithMany().HasForeignKey(a => a.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Domain.Modules.Environments.Environment>().WithMany().HasForeignKey(a => a.EnvironmentId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.UserId, a.RoleId, a.EnvironmentId }).IsUnique();
    }
}

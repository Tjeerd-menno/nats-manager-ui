namespace NatsManager.Domain.Modules.Auth;

public sealed class UserRoleAssignment
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? EnvironmentId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Guid AssignedBy { get; private set; }

    private UserRoleAssignment() { }

    public static UserRoleAssignment Create(Guid userId, Guid roleId, Guid? environmentId, Guid assignedBy)
    {
        return new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            EnvironmentId = environmentId,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }
}

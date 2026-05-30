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
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required.", nameof(userId));
        if (roleId == Guid.Empty)
            throw new ArgumentException("Role id is required.", nameof(roleId));
        if (assignedBy == Guid.Empty)
            throw new ArgumentException("AssignedBy is required.", nameof(assignedBy));
        if (environmentId == Guid.Empty)
            throw new ArgumentException("Environment id must be null (global) or a non-empty value.", nameof(environmentId));

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

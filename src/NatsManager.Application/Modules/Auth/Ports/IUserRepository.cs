using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Application.Modules.Auth.Ports;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRoleAssignment>> GetUserRoleAssignmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken cancellationToken = default);
    Task RemoveRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}

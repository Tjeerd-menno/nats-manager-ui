using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Ports;

namespace NatsManager.Application.Modules.Auth.Queries;

public sealed record UserDto(Guid Id, string Username, string DisplayName, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt);
public sealed record RoleDto(Guid Id, string Name, string Description);
public sealed record UserRoleDto(Guid AssignmentId, Guid RoleId, string RoleName, Guid? EnvironmentId, DateTime AssignedAt);

public sealed record GetUsersQuery;

public sealed class GetUsersQueryHandler(IUserRepository userRepository) : IUseCase<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task ExecuteAsync(GetUsersQuery request, IOutputPort<IReadOnlyList<UserDto>> outputPort, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        outputPort.Success([.. users.Select(u => new UserDto(u.Id, u.Username, u.DisplayName, u.IsActive, u.CreatedAt, u.LastLoginAt))]);
    }
}

public sealed record GetRolesQuery;

public sealed class GetRolesQueryHandler(IUserRepository userRepository) : IUseCase<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    public async Task ExecuteAsync(GetRolesQuery request, IOutputPort<IReadOnlyList<RoleDto>> outputPort, CancellationToken cancellationToken)
    {
        var roles = await userRepository.GetRolesAsync(cancellationToken);
        outputPort.Success([.. roles.Select(r => new RoleDto(r.Id, r.Name, r.Description))]);
    }
}

public sealed record GetUserRolesQuery(Guid UserId);

public sealed class GetUserRolesQueryHandler(IUserRepository userRepository) : IUseCase<GetUserRolesQuery, IReadOnlyList<UserRoleDto>>
{
    public async Task ExecuteAsync(GetUserRolesQuery request, IOutputPort<IReadOnlyList<UserRoleDto>> outputPort, CancellationToken cancellationToken)
    {
        var assignments = await userRepository.GetUserRoleAssignmentsAsync(request.UserId, cancellationToken);
        var roles = await userRepository.GetRolesAsync(cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name);

        outputPort.Success([.. assignments.Select(a => new UserRoleDto(
            a.Id,
            a.RoleId,
            roleMap.GetValueOrDefault(a.RoleId, "Unknown"),
            a.EnvironmentId,
            a.AssignedAt))]);
    }
}

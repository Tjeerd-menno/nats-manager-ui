using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Commands;
using NatsManager.Application.Modules.Auth.Queries;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;
using System.Security.Claims;

namespace NatsManager.Web.Endpoints;

public static class AccessControlEndpoints
{
    public static void MapAccessControlEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/access-control")
            .RequireAuthorization(AuthorizationPolicyNames.AdminOnly);

        group.MapGet("/users", GetUsers);
        group.MapPost("/users", CreateUser);
        group.MapPut("/users/{userId:guid}", UpdateUser);
        group.MapDelete("/users/{userId:guid}", DeactivateUser);
        group.MapGet("/users/{userId:guid}/roles", GetUserRoles);
        group.MapPost("/users/{userId:guid}/roles", AssignRole);
        group.MapDelete("/users/{userId:guid}/roles/{assignmentId:guid}", RevokeRole);
        group.MapGet("/roles", GetRoles);
    }

    private static async Task<IResult> GetUsers(IUseCase<GetUsersQuery, IReadOnlyList<UserDto>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<UserDto>>();
        await useCase.ExecuteAsync(new GetUsersQuery(), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> CreateUser(CreateUserCommand command, IUseCase<CreateUserCommand, Guid> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Guid>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Created($"/api/access-control/users/{presenter.Value}", new { Id = presenter.Value });
        return presenter.ToResult();
    }

    private static async Task<IResult> UpdateUser(Guid userId, UpdateUserRequest request, IUseCase<UpdateUserCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new UpdateUserCommand { UserId = userId, DisplayName = request.DisplayName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> DeactivateUser(Guid userId, IUseCase<DeactivateUserCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeactivateUserCommand { UserId = userId }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> GetUserRoles(Guid userId, IUseCase<GetUserRolesQuery, IReadOnlyList<UserRoleDto>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<UserRoleDto>>();
        await useCase.ExecuteAsync(new GetUserRolesQuery(userId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> AssignRole(Guid userId, AssignRoleRequest request, IUseCase<AssignRoleCommand, Unit> useCase, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var actorIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actorIdValue, out var actorId))
        {
            return Results.Unauthorized();
        }

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new AssignRoleCommand
        {
            UserId = userId,
            RoleId = request.RoleId,
            EnvironmentId = request.EnvironmentId,
            AssignedBy = actorId
        }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> RevokeRole(Guid userId, Guid assignmentId, IUseCase<RevokeRoleCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new RevokeRoleCommand { UserId = userId, AssignmentId = assignmentId }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> GetRoles(IUseCase<GetRolesQuery, IReadOnlyList<RoleDto>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<RoleDto>>();
        await useCase.ExecuteAsync(new GetRolesQuery(), presenter, cancellationToken);
        return presenter.ToResult();
    }
}

public sealed record UpdateUserRequest(string DisplayName);
public sealed record AssignRoleRequest(Guid RoleId, Guid? EnvironmentId);

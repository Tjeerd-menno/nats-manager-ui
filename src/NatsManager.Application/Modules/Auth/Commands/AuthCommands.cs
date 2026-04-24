using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Ports;
using NatsManager.Domain.Modules.Auth;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Auth.Commands;

public sealed record LoginCommand(string Username, string Password);

public sealed record LoginResult(Guid Id, string Username, string DisplayName, IReadOnlyList<string> Roles);

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAuditTrail auditTrail) : IUseCase<LoginCommand, LoginResult>
{
    // Generic message returned to the client for any failure so that we don't leak
    // whether the username exists or whether an account is locked/disabled.
    private const string GenericFailureMessage = "Invalid credentials.";

    public async Task ExecuteAsync(LoginCommand request, IOutputPort<LoginResult> outputPort, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            await RecordFailureAsync(request.Username, "User not found.", cancellationToken);
            outputPort.Unauthorized(GenericFailureMessage);
            return;
        }

        if (!user.IsActive)
        {
            await RecordFailureAsync(request.Username, "Account is disabled.", cancellationToken);
            outputPort.Unauthorized(GenericFailureMessage);
            return;
        }

        if (user.IsLocked())
        {
            await RecordFailureAsync(request.Username, "Account is locked.", cancellationToken);
            outputPort.Unauthorized(GenericFailureMessage);
            return;
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await userRepository.UpdateAsync(user, cancellationToken);
            await RecordFailureAsync(request.Username, "Password mismatch.", cancellationToken);
            outputPort.Unauthorized(GenericFailureMessage);
            return;
        }

        user.RecordLogin();
        await userRepository.UpdateAsync(user, cancellationToken);

        await auditTrail.RecordAsync(
            ActionType.Login,
            ResourceType.User,
            user.Id.ToString(),
            user.Username,
            environmentId: null,
            Outcome.Success,
            details: null,
            AuditSource.UserInitiated,
            cancellationToken);

        var assignments = await userRepository.GetUserRoleAssignmentsAsync(user.Id, cancellationToken);
        var roles = await userRepository.GetRolesAsync(cancellationToken);
        var roleMap = roles.ToDictionary(r => r.Id, r => r.Name);
        var userRoles = assignments.Select(a => roleMap.GetValueOrDefault(a.RoleId, "Unknown")).Distinct().ToList();

        outputPort.Success(new LoginResult(user.Id, user.Username, user.DisplayName, userRoles));
    }

    private Task RecordFailureAsync(string username, string details, CancellationToken cancellationToken)
        => auditTrail.RecordAsync(
            ActionType.Login,
            ResourceType.User,
            resourceId: string.IsNullOrWhiteSpace(username) ? "unknown" : username,
            resourceName: string.IsNullOrWhiteSpace(username) ? "unknown" : username,
            environmentId: null,
            Outcome.Failure,
            details: details,
            AuditSource.UserInitiated,
            cancellationToken);
}

public sealed class CreateUserCommand : IAuditableCommand
{
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.User;
    string IAuditableCommand.ResourceId => Username;
    string IAuditableCommand.ResourceName => DisplayName;
    Guid? IAuditableCommand.EnvironmentId => null;
}

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(256)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^A-Za-z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
    }
}

public sealed class CreateUserCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IAuditTrail auditTrail) : IUseCase<CreateUserCommand, Guid>
{
    public async Task ExecuteAsync(CreateUserCommand request, IOutputPort<Guid> outputPort, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existing is not null)
        {
            outputPort.Conflict($"User '{request.Username}' already exists.");
            return;
        }

        var hash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Username, request.DisplayName, hash);
        await userRepository.AddAsync(user, cancellationToken);
        await auditTrail.RecordAsync(request, user.Id.ToString(), cancellationToken);
        outputPort.Success(user.Id);
    }
}

public sealed class UpdateUserCommand : IAuditableCommand
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    ActionType IAuditableCommand.ActionType => ActionType.Update;
    ResourceType IAuditableCommand.ResourceType => ResourceType.User;
    string IAuditableCommand.ResourceId => UserId.ToString();
    string IAuditableCommand.ResourceName => DisplayName;
    Guid? IAuditableCommand.EnvironmentId => null;
}

public sealed class UpdateUserCommandHandler(IUserRepository userRepository, IAuditTrail auditTrail) : IUseCase<UpdateUserCommand, Unit>
{
    public async Task ExecuteAsync(UpdateUserCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            outputPort.NotFound("User", request.UserId.ToString());
            return;
        }

        user.UpdateProfile(request.DisplayName);
        await userRepository.UpdateAsync(user, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class DeactivateUserCommand : IAuditableCommand
{
    public Guid UserId { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.User;
    string IAuditableCommand.ResourceId => UserId.ToString();
    string IAuditableCommand.ResourceName => UserId.ToString();
    Guid? IAuditableCommand.EnvironmentId => null;
}

public sealed class DeactivateUserCommandHandler(IUserRepository userRepository, IAuditTrail auditTrail) : IUseCase<DeactivateUserCommand, Unit>
{
    public async Task ExecuteAsync(DeactivateUserCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            outputPort.NotFound("User", request.UserId.ToString());
            return;
        }

        user.Deactivate();
        await userRepository.UpdateAsync(user, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class AssignRoleCommand : IAuditableCommand
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public Guid? EnvironmentId { get; init; }
    public Guid AssignedBy { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.PermissionChange;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Role;
    string IAuditableCommand.ResourceId => $"{UserId}:{RoleId}";
    string IAuditableCommand.ResourceName => $"Role assignment for user {UserId}";
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class AssignRoleCommandHandler(IUserRepository userRepository, IAuditTrail auditTrail) : IUseCase<AssignRoleCommand, Unit>
{
    public async Task ExecuteAsync(AssignRoleCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var assignment = UserRoleAssignment.Create(request.UserId, request.RoleId, request.EnvironmentId, request.AssignedBy);
        await userRepository.AddRoleAssignmentAsync(assignment, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class RevokeRoleCommand : IAuditableCommand
{
    public Guid UserId { get; init; }
    public Guid AssignmentId { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.PermissionChange;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Role;
    string IAuditableCommand.ResourceId => AssignmentId.ToString();
    string IAuditableCommand.ResourceName => $"Role revocation {AssignmentId}";
    Guid? IAuditableCommand.EnvironmentId => null;
}

public sealed class RevokeRoleCommandHandler(IUserRepository userRepository, IAuditTrail auditTrail) : IUseCase<RevokeRoleCommand, Unit>
{
    public async Task ExecuteAsync(RevokeRoleCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var assignments = await userRepository.GetUserRoleAssignmentsAsync(request.UserId, cancellationToken);
        if (!assignments.Any(a => a.Id == request.AssignmentId))
        {
            outputPort.NotFound("RoleAssignment", request.AssignmentId.ToString());
            return;
        }

        await userRepository.RemoveRoleAssignmentAsync(request.AssignmentId, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

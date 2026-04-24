using Shouldly;
using NSubstitute;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Auth.Commands;
using NatsManager.Application.Modules.Auth.Ports;
using NatsManager.Application.Modules.Auth.Queries;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Application.Tests.Modules.Auth;

public sealed class LoginCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandTests()
    {
        _handler = new LoginCommandHandler(_userRepo, _hasher);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnLoginResult()
    {
        var user = User.Create("admin", "Admin User", "hashed");
        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("password123", "hashed").Returns(true);
        _userRepo.GetUserRoleAssignmentsAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        _userRepo.GetRolesAsync(Arg.Any<CancellationToken>()).Returns([]);

        var outputPort = new TestOutputPort<LoginResult>();
        await _handler.ExecuteAsync(new LoginCommand("admin", "password123"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Id.ShouldBe(user.Id);
        outputPort.Value!.Username.ShouldBe("admin");
        outputPort.Value!.DisplayName.ShouldBe("Admin User");
        outputPort.Value!.Roles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidLogin_ShouldRecordLoginAndUpdate()
    {
        var user = User.Create("admin", "Admin User", "hashed");
        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("password123", "hashed").Returns(true);
        _userRepo.GetUserRoleAssignmentsAsync(user.Id, Arg.Any<CancellationToken>()).Returns([]);
        _userRepo.GetRolesAsync(Arg.Any<CancellationToken>()).Returns([]);

        var outputPort = new TestOutputPort<LoginResult>();
        await _handler.ExecuteAsync(new LoginCommand("admin", "password123"), outputPort, CancellationToken.None);

        user.LastLoginAt.ShouldNotBeNull();
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldBeUnauthorized()
    {
        _userRepo.GetByUsernameAsync("unknown", Arg.Any<CancellationToken>()).Returns((User?)null);

        var outputPort = new TestOutputPort<LoginResult>();
        await _handler.ExecuteAsync(new LoginCommand("unknown", "pass"), outputPort, CancellationToken.None);

        outputPort.IsUnauthorized.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ShouldBeUnauthorized()
    {
        var user = User.Create("admin", "Admin", "hashed");
        user.Deactivate();
        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);

        var outputPort = new TestOutputPort<LoginResult>();
        await _handler.ExecuteAsync(new LoginCommand("admin", "pass"), outputPort, CancellationToken.None);

        outputPort.IsUnauthorized.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenPasswordWrong_ShouldBeUnauthorized()
    {
        var user = User.Create("admin", "Admin", "hashed");
        _userRepo.GetByUsernameAsync("admin", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong", "hashed").Returns(false);

        var outputPort = new TestOutputPort<LoginResult>();
        await _handler.ExecuteAsync(new LoginCommand("admin", "wrong"), outputPort, CancellationToken.None);

        outputPort.IsUnauthorized.ShouldBeTrue();
    }
}

public sealed class CreateUserCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandTests()
    {
        _handler = new CreateUserCommandHandler(_userRepo, _hasher, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldHashPasswordAndCreateUser()
    {
        _userRepo.GetByUsernameAsync("newuser", Arg.Any<CancellationToken>()).Returns((User?)null);
        _hasher.Hash("password123").Returns("hashed-pw");

        var outputPort = new TestOutputPort<Guid>();
        await _handler.ExecuteAsync(
            new CreateUserCommand { Username = "newuser", DisplayName = "New User", Password = "password123" },
            outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.ShouldNotBe(Guid.Empty);
        await _userRepo.Received(1).AddAsync(Arg.Is<User>(u => u.Username == "newuser" && u.PasswordHash == "hashed-pw"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUsernameExists_ShouldBeConflict()
    {
        var existing = User.Create("taken", "Taken", "hash");
        _userRepo.GetByUsernameAsync("taken", Arg.Any<CancellationToken>()).Returns(existing);

        var outputPort = new TestOutputPort<Guid>();
        await _handler.ExecuteAsync(
            new CreateUserCommand { Username = "taken", DisplayName = "User", Password = "password123" },
            outputPort, CancellationToken.None);

        outputPort.IsConflict.ShouldBeTrue();
    }
}

public sealed class UpdateUserCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandTests()
    {
        _handler = new UpdateUserCommandHandler(_userRepo, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldUpdateDisplayName()
    {
        var user = User.Create("admin", "Old Name", "hash");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new UpdateUserCommand { UserId = user.Id, DisplayName = "New Name" }, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        user.DisplayName.ShouldBe("New Name");
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldBeNotFound()
    {
        var id = Guid.NewGuid();
        _userRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new UpdateUserCommand { UserId = id, DisplayName = "Name" }, outputPort, CancellationToken.None);

        outputPort.IsNotFound.ShouldBeTrue();
    }
}

public sealed class DeactivateUserCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeactivateUserCommandHandler _handler;

    public DeactivateUserCommandTests()
    {
        _handler = new DeactivateUserCommandHandler(_userRepo, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDeactivateUser()
    {
        var user = User.Create("admin", "Admin", "hash");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new DeactivateUserCommand { UserId = user.Id }, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        user.IsActive.ShouldBeFalse();
        await _userRepo.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldBeNotFound()
    {
        var id = Guid.NewGuid();
        _userRepo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((User?)null);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new DeactivateUserCommand { UserId = id }, outputPort, CancellationToken.None);

        outputPort.IsNotFound.ShouldBeTrue();
    }
}

public sealed class AssignRoleCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly AssignRoleCommandHandler _handler;

    public AssignRoleCommandTests()
    {
        _handler = new AssignRoleCommandHandler(_userRepo, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldCreateAssignment()
    {
        var command = new AssignRoleCommand
        {
            UserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            AssignedBy = Guid.NewGuid()
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _userRepo.Received(1).AddRoleAssignmentAsync(
            Arg.Is<UserRoleAssignment>(a => a.UserId == command.UserId && a.RoleId == command.RoleId),
            Arg.Any<CancellationToken>());
    }
}

public sealed class RevokeRoleCommandTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly RevokeRoleCommandHandler _handler;

    public RevokeRoleCommandTests()
    {
        _handler = new RevokeRoleCommandHandler(_userRepo, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAssignment()
    {
        var userId = Guid.NewGuid();
        var assignment = UserRoleAssignment.Create(userId, Guid.NewGuid(), environmentId: null, assignedBy: userId);
        var assignmentId = assignment.Id;
        _userRepo.GetUserRoleAssignmentsAsync(userId, Arg.Any<CancellationToken>())
            .Returns([assignment]);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new RevokeRoleCommand { UserId = userId, AssignmentId = assignmentId }, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _userRepo.Received(1).RemoveRoleAssignmentAsync(assignmentId, Arg.Any<CancellationToken>());
    }
}

public sealed class GetUsersQueryTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryTests()
    {
        _handler = new GetUsersQueryHandler(_userRepo);
    }

    [Fact]
    public async Task Handle_ShouldMapUsersToDto()
    {
        var user = User.Create("admin", "Admin User", "hash");
        _userRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        var outputPort = new TestOutputPort<IReadOnlyList<UserDto>>();
        await _handler.ExecuteAsync(new GetUsersQuery(), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(1);
        outputPort.Value![0].Username.ShouldBe("admin");
        outputPort.Value![0].DisplayName.ShouldBe("Admin User");
        outputPort.Value![0].IsActive.ShouldBeTrue();
    }
}

public sealed class GetRolesQueryTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly GetRolesQueryHandler _handler;

    public GetRolesQueryTests()
    {
        _handler = new GetRolesQueryHandler(_userRepo);
    }

    [Fact]
    public async Task Handle_ShouldMapRolesToDto()
    {
        var role = Role.Create("Administrator", "Full access");
        _userRepo.GetRolesAsync(Arg.Any<CancellationToken>()).Returns(new List<Role> { role });

        var outputPort = new TestOutputPort<IReadOnlyList<RoleDto>>();
        await _handler.ExecuteAsync(new GetRolesQuery(), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(1);
        outputPort.Value![0].Name.ShouldBe("Administrator");
        outputPort.Value![0].Description.ShouldBe("Full access");
    }
}

public sealed class GetUserRolesQueryTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly GetUserRolesQueryHandler _handler;

    public GetUserRolesQueryTests()
    {
        _handler = new GetUserRolesQueryHandler(_userRepo);
    }

    [Fact]
    public async Task Handle_ShouldMapAssignmentsWithRoleNames()
    {
        var userId = Guid.NewGuid();
        var role = Role.Create("Admin", "Full access");
        var assignment = UserRoleAssignment.Create(userId, role.Id, null, Guid.NewGuid());

        _userRepo.GetUserRoleAssignmentsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<UserRoleAssignment> { assignment });
        _userRepo.GetRolesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Role> { role });

        var outputPort = new TestOutputPort<IReadOnlyList<UserRoleDto>>();
        await _handler.ExecuteAsync(new GetUserRolesQuery(userId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(1);
        outputPort.Value![0].RoleName.ShouldBe("Admin");
        outputPort.Value![0].RoleId.ShouldBe(role.Id);
    }
}

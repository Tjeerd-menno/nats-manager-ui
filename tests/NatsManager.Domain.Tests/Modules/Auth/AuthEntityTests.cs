using FluentAssertions;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Domain.Tests.Modules.Auth;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateUser()
    {
        var user = User.Create("admin", "Admin User", "hashed-password");

        user.Id.Should().NotBeEmpty();
        user.Username.Should().Be("admin");
        user.DisplayName.Should().Be("Admin User");
        user.PasswordHash.Should().Be("hashed-password");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        user.LastLoginAt.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUsername_ShouldThrow(string? username)
    {
        var act = () => User.Create(username!, "Display", "hash");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordFailedLogin_ShouldIncrementCounter()
    {
        var user = User.Create("admin", "Admin", "hash");

        user.RecordFailedLogin();

        user.FailedLoginAttempts.Should().Be(1);
        user.IsLocked().Should().BeFalse();
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_WhenThresholdReached_ShouldLockAccount()
    {
        var user = User.Create("admin", "Admin", "hash");

        for (var i = 0; i < User.DefaultLockoutThreshold; i++)
        {
            user.RecordFailedLogin();
        }

        user.FailedLoginAttempts.Should().Be(User.DefaultLockoutThreshold);
        user.IsLocked().Should().BeTrue();
        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil!.Value.Should().BeCloseTo(
            DateTimeOffset.UtcNow.Add(User.DefaultLockoutDuration),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsLocked_WhenLockoutExpired_ShouldReturnFalse()
    {
        var user = User.Create("admin", "Admin", "hash");
        for (var i = 0; i < User.DefaultLockoutThreshold; i++)
        {
            user.RecordFailedLogin(threshold: User.DefaultLockoutThreshold, lockoutDuration: TimeSpan.FromMilliseconds(1));
        }

        user.IsLocked(DateTimeOffset.UtcNow.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void RecordLogin_ShouldResetFailedAttemptsAndClearLock()
    {
        var user = User.Create("admin", "Admin", "hash");
        for (var i = 0; i < User.DefaultLockoutThreshold; i++)
        {
            user.RecordFailedLogin();
        }

        user.RecordLogin();

        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.IsLocked().Should().BeFalse();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailedLogin_WithInvalidThreshold_ShouldThrow()
    {
        var user = User.Create("admin", "Admin", "hash");
        var act = () => user.RecordFailedLogin(threshold: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordFailedLogin_WithNonPositiveLockoutDuration_ShouldThrow(int seconds)
    {
        var user = User.Create("admin", "Admin", "hash");
        var act = () => user.RecordFailedLogin(lockoutDuration: TimeSpan.FromSeconds(seconds));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => User.Create("admin", displayName!, "hash");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPasswordHash_ShouldThrow(string? hash)
    {
        var act = () => User.Create("admin", "Display", hash!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithUsernameTooLong_ShouldThrow()
    {
        var act = () => User.Create(new string('u', 101), "Display", "hash");
        act.Should().Throw<ArgumentException>().WithMessage("*100 characters*");
    }

    [Fact]
    public void Create_ShouldTrimUsernameAndDisplayName()
    {
        var user = User.Create("  admin  ", "  Admin  ", "hash");
        user.Username.Should().Be("admin");
        user.DisplayName.Should().Be("Admin");
    }

    [Fact]
    public void UpdateProfile_ShouldChangeDisplayName()
    {
        var user = User.Create("admin", "Old Name", "hash");
        user.UpdateProfile("New Name");
        user.DisplayName.Should().Be("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidDisplayName_ShouldThrow(string? name)
    {
        var user = User.Create("admin", "Display", "hash");
        var act = () => user.UpdateProfile(name!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdatePassword_ShouldChangeHash()
    {
        var user = User.Create("admin", "Admin", "old-hash");
        user.UpdatePassword("new-hash");
        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.LastLoginAt.Should().BeNull();

        user.RecordLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Activate_ShouldSetActive()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.Deactivate();
        user.Activate();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }
}

public sealed class RoleTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateRole()
    {
        var role = Role.Create("Admin", "Administrator role");

        role.Id.Should().NotBeEmpty();
        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator role");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        var act = () => Role.Create(name!, "desc");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PredefinedNames_ShouldHaveExpectedValues()
    {
        Role.PredefinedNames.ReadOnly.Should().Be("ReadOnly");
        Role.PredefinedNames.Operator.Should().Be("Operator");
        Role.PredefinedNames.Administrator.Should().Be("Administrator");
        Role.PredefinedNames.Auditor.Should().Be("Auditor");
    }
}

public sealed class UserRoleAssignmentTests
{
    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var assignedBy = Guid.NewGuid();

        var assignment = UserRoleAssignment.Create(userId, roleId, envId, assignedBy);

        assignment.Id.Should().NotBeEmpty();
        assignment.UserId.Should().Be(userId);
        assignment.RoleId.Should().Be(roleId);
        assignment.EnvironmentId.Should().Be(envId);
        assignment.AssignedBy.Should().Be(assignedBy);
        assignment.AssignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullEnvironmentId_ShouldAllowGlobalAssignment()
    {
        var assignment = UserRoleAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());

        assignment.EnvironmentId.Should().BeNull();
    }
}

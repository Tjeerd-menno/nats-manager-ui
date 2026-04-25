using Shouldly;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Domain.Tests.Modules.Auth;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateUser()
    {
        var user = User.Create("admin", "Admin User", "hashed-password");

        user.Id.ShouldNotBe(Guid.Empty);
        user.Username.ShouldBe("admin");
        user.DisplayName.ShouldBe("Admin User");
        user.PasswordHash.ShouldBe("hashed-password");
        user.IsActive.ShouldBeTrue();
        (user.CreatedAt - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
        user.LastLoginAt.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUsername_ShouldThrow(string? username)
    {
        var act = () => User.Create(username!, "Display", "hash");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void RecordFailedLogin_ShouldIncrementCounter()
    {
        var user = User.Create("admin", "Admin", "hash");

        user.RecordFailedLogin();

        user.FailedLoginAttempts.ShouldBe(1);
        user.IsLocked().ShouldBeFalse();
        user.LockedUntil.ShouldBeNull();
    }

    [Fact]
    public void RecordFailedLogin_WhenThresholdReached_ShouldLockAccount()
    {
        var user = User.Create("admin", "Admin", "hash");

        for (var i = 0; i < User.DefaultLockoutThreshold; i++)
        {
            user.RecordFailedLogin();
        }

        user.FailedLoginAttempts.ShouldBe(User.DefaultLockoutThreshold);
        user.IsLocked().ShouldBeTrue();
        user.LockedUntil.ShouldNotBeNull();
        (user.LockedUntil!.Value - DateTimeOffset.UtcNow.Add(User.DefaultLockoutDuration)).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IsLocked_WhenLockoutExpired_ShouldReturnFalse()
    {
        var user = User.Create("admin", "Admin", "hash");
        for (var i = 0; i < User.DefaultLockoutThreshold; i++)
        {
            user.RecordFailedLogin(threshold: User.DefaultLockoutThreshold, lockoutDuration: TimeSpan.FromMilliseconds(1));
        }

        user.IsLocked(DateTimeOffset.UtcNow.AddHours(1)).ShouldBeFalse();
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

        user.FailedLoginAttempts.ShouldBe(0);
        user.LockedUntil.ShouldBeNull();
        user.IsLocked().ShouldBeFalse();
        user.LastLoginAt.ShouldNotBeNull();
    }

    [Fact]
    public void RecordFailedLogin_WithInvalidThreshold_ShouldThrow()
    {
        var user = User.Create("admin", "Admin", "hash");
        var act = () => user.RecordFailedLogin(threshold: 0);
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RecordFailedLogin_WithNonPositiveLockoutDuration_ShouldThrow(int seconds)
    {
        var user = User.Create("admin", "Admin", "hash");
        var act = () => user.RecordFailedLogin(lockoutDuration: TimeSpan.FromSeconds(seconds));
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => User.Create("admin", displayName!, "hash");
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidPasswordHash_ShouldThrow(string? hash)
    {
        var act = () => User.Create("admin", "Display", hash!);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithUsernameTooLong_ShouldThrow()
    {
        var act = () => User.Create(new string('u', 101), "Display", "hash");
        Should.Throw<ArgumentException>(act).Message.ShouldContain("100 characters");
    }

    [Fact]
    public void Create_ShouldTrimUsernameAndDisplayName()
    {
        var user = User.Create("  admin  ", "  Admin  ", "hash");
        user.Username.ShouldBe("admin");
        user.DisplayName.ShouldBe("Admin");
    }

    [Fact]
    public void UpdateProfile_ShouldChangeDisplayName()
    {
        var user = User.Create("admin", "Old Name", "hash");
        user.UpdateProfile("New Name");
        user.DisplayName.ShouldBe("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateProfile_WithInvalidDisplayName_ShouldThrow(string? name)
    {
        var user = User.Create("admin", "Display", "hash");
        var act = () => user.UpdateProfile(name!);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void UpdatePassword_ShouldChangeHash()
    {
        var user = User.Create("admin", "Admin", "old-hash");
        user.UpdatePassword("new-hash");
        user.PasswordHash.ShouldBe("new-hash");
    }

    [Fact]
    public void RecordLogin_ShouldSetLastLoginAt()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.LastLoginAt.ShouldBeNull();

        user.RecordLogin();

        user.LastLoginAt.ShouldNotBeNull();
        (user.LastLoginAt!.Value - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Activate_ShouldSetActive()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.Deactivate();
        user.Activate();
        user.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetInactive()
    {
        var user = User.Create("admin", "Admin", "hash");
        user.Deactivate();
        user.IsActive.ShouldBeFalse();
    }
}

public sealed class RoleTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateRole()
    {
        var role = Role.Create("Admin", "Administrator role");

        role.Id.ShouldNotBe(Guid.Empty);
        role.Name.ShouldBe("Admin");
        role.Description.ShouldBe("Administrator role");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? name)
    {
        var act = () => Role.Create(name!, "desc");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void PredefinedNames_ShouldHaveExpectedValues()
    {
        Role.PredefinedNames.ReadOnly.ShouldBe("ReadOnly");
        Role.PredefinedNames.Operator.ShouldBe("Operator");
        Role.PredefinedNames.Administrator.ShouldBe("Administrator");
        Role.PredefinedNames.Auditor.ShouldBe("Auditor");
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

        assignment.Id.ShouldNotBe(Guid.Empty);
        assignment.UserId.ShouldBe(userId);
        assignment.RoleId.ShouldBe(roleId);
        assignment.EnvironmentId.ShouldBe(envId);
        assignment.AssignedBy.ShouldBe(assignedBy);
        (assignment.AssignedAt - DateTime.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullEnvironmentId_ShouldAllowGlobalAssignment()
    {
        var assignment = UserRoleAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid());

        assignment.EnvironmentId.ShouldBeNull();
    }
}

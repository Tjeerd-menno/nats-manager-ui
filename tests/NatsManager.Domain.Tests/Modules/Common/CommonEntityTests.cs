using Shouldly;
using NatsManager.Domain.Modules.Common;
using NatsManager.Domain.Modules.Common.Errors;

namespace NatsManager.Domain.Tests.Modules.Common;

public sealed class BookmarkTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateBookmark()
    {
        var userId = Guid.NewGuid();
        var envId = Guid.NewGuid();

        var bookmark = Bookmark.Create(userId, envId, ResourceType.Stream, "stream-1", "My Stream");

        bookmark.Id.ShouldNotBe(Guid.Empty);
        bookmark.UserId.ShouldBe(userId);
        bookmark.EnvironmentId.ShouldBe(envId);
        bookmark.ResourceType.ShouldBe(ResourceType.Stream);
        bookmark.ResourceId.ShouldBe("stream-1");
        bookmark.DisplayName.ShouldBe("My Stream");
        (bookmark.CreatedAt - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidResourceId_ShouldThrow(string? resourceId)
    {
        var act = () => Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, resourceId!, "Name");
        Should.Throw<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", displayName!);
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void UpdateDisplayName_WithValidName_ShouldUpdate()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", "Old Name");

        bookmark.UpdateDisplayName("New Name");

        bookmark.DisplayName.ShouldBe("New Name");
    }

    [Fact]
    public void UpdateDisplayName_WithInvalidName_ShouldThrow()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", "Name");

        var act = () => bookmark.UpdateDisplayName("");

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimFields()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "  id  ", "  Name  ");

        bookmark.ResourceId.ShouldBe("id");
        bookmark.DisplayName.ShouldBe("Name");
    }
}

public sealed class UserPreferenceTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreatePreference()
    {
        var userId = Guid.NewGuid();

        var pref = UserPreference.Create(userId, "theme", "dark");

        pref.Id.ShouldNotBe(Guid.Empty);
        pref.UserId.ShouldBe(userId);
        pref.Key.ShouldBe("theme");
        pref.Value.ShouldBe("dark");
        (pref.UpdatedAt - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidKey_ShouldThrow(string? key)
    {
        var act = () => UserPreference.Create(Guid.NewGuid(), key!, "value");
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithNullValue_ShouldDefaultToEmpty()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "key", null!);
        pref.Value.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateValue_ShouldChangeValueAndTimestamp()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "theme", "light");
        var originalUpdatedAt = pref.UpdatedAt;

        pref.UpdateValue("dark");

        pref.Value.ShouldBe("dark");
        pref.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Fact]
    public void UpdateValue_WithNull_ShouldSetEmpty()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "key", "value");

        pref.UpdateValue(null!);

        pref.Value.ShouldBeEmpty();
    }
}

public sealed class DomainExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldContainResourceInfo()
    {
        var ex = new NotFoundException("Stream", "my-stream");

        ex.Message.ShouldContain("Stream");
        ex.Message.ShouldContain("my-stream");
        ex.ErrorCode.ShouldBe("RESOURCE_NOT_FOUND");
        ex.ResourceType.ShouldBe("Stream");
        ex.ResourceId.ShouldBe("my-stream");
    }

    [Fact]
    public void ConflictException_ShouldContainMessage()
    {
        var ex = new ConflictException("Already exists");

        ex.Message.ShouldBe("Already exists");
        ex.ErrorCode.ShouldBe("CONFLICT");
    }

    [Fact]
    public void ForbiddenException_ShouldContainMessage()
    {
        var ex = new ForbiddenException("Not allowed");

        ex.Message.ShouldBe("Not allowed");
        ex.ErrorCode.ShouldBe("FORBIDDEN");
    }

    [Fact]
    public void ConnectionException_ShouldContainEnvironmentInfo()
    {
        var ex = new ConnectionException("prod", "timeout");

        ex.Message.ShouldContain("prod");
        ex.Message.ShouldContain("timeout");
        ex.ErrorCode.ShouldBe("CONNECTION_ERROR");
        ex.EnvironmentName.ShouldBe("prod");
    }
}

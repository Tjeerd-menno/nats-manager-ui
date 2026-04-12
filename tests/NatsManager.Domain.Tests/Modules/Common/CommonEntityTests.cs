using FluentAssertions;
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

        bookmark.Id.Should().NotBeEmpty();
        bookmark.UserId.Should().Be(userId);
        bookmark.EnvironmentId.Should().Be(envId);
        bookmark.ResourceType.Should().Be(ResourceType.Stream);
        bookmark.ResourceId.Should().Be("stream-1");
        bookmark.DisplayName.Should().Be("My Stream");
        bookmark.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidResourceId_ShouldThrow(string? resourceId)
    {
        var act = () => Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, resourceId!, "Name");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDisplayName_ShouldThrow(string? displayName)
    {
        var act = () => Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", displayName!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDisplayName_WithValidName_ShouldUpdate()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", "Old Name");

        bookmark.UpdateDisplayName("New Name");

        bookmark.DisplayName.Should().Be("New Name");
    }

    [Fact]
    public void UpdateDisplayName_WithInvalidName_ShouldThrow()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", "Name");

        var act = () => bookmark.UpdateDisplayName("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimFields()
    {
        var bookmark = Bookmark.Create(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "  id  ", "  Name  ");

        bookmark.ResourceId.Should().Be("id");
        bookmark.DisplayName.Should().Be("Name");
    }
}

public sealed class UserPreferenceTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreatePreference()
    {
        var userId = Guid.NewGuid();

        var pref = UserPreference.Create(userId, "theme", "dark");

        pref.Id.Should().NotBeEmpty();
        pref.UserId.Should().Be(userId);
        pref.Key.Should().Be("theme");
        pref.Value.Should().Be("dark");
        pref.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidKey_ShouldThrow(string? key)
    {
        var act = () => UserPreference.Create(Guid.NewGuid(), key!, "value");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNullValue_ShouldDefaultToEmpty()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "key", null!);
        pref.Value.Should().BeEmpty();
    }

    [Fact]
    public void UpdateValue_ShouldChangeValueAndTimestamp()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "theme", "light");
        var originalUpdatedAt = pref.UpdatedAt;

        pref.UpdateValue("dark");

        pref.Value.Should().Be("dark");
        pref.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    [Fact]
    public void UpdateValue_WithNull_ShouldSetEmpty()
    {
        var pref = UserPreference.Create(Guid.NewGuid(), "key", "value");

        pref.UpdateValue(null!);

        pref.Value.Should().BeEmpty();
    }
}

public sealed class DomainExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldContainResourceInfo()
    {
        var ex = new NotFoundException("Stream", "my-stream");

        ex.Message.Should().Contain("Stream");
        ex.Message.Should().Contain("my-stream");
        ex.ErrorCode.Should().Be("RESOURCE_NOT_FOUND");
        ex.ResourceType.Should().Be("Stream");
        ex.ResourceId.Should().Be("my-stream");
    }

    [Fact]
    public void ConflictException_ShouldContainMessage()
    {
        var ex = new ConflictException("Already exists");

        ex.Message.Should().Be("Already exists");
        ex.ErrorCode.Should().Be("CONFLICT");
    }

    [Fact]
    public void ForbiddenException_ShouldContainMessage()
    {
        var ex = new ForbiddenException("Not allowed");

        ex.Message.Should().Be("Not allowed");
        ex.ErrorCode.Should().Be("FORBIDDEN");
    }

    [Fact]
    public void ConnectionException_ShouldContainEnvironmentInfo()
    {
        var ex = new ConnectionException("prod", "timeout");

        ex.Message.Should().Contain("prod");
        ex.Message.Should().Contain("timeout");
        ex.ErrorCode.Should().Be("CONNECTION_ERROR");
        ex.EnvironmentName.Should().Be("prod");
    }
}

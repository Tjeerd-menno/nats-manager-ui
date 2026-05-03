using Shouldly;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Search.Commands;
using NatsManager.Application.Modules.Search.Ports;
using NatsManager.Application.Modules.Search.Queries;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Tests.Modules.Search;

public sealed class AddBookmarkCommandTests
{
    private readonly IBookmarkRepository _repository = Substitute.For<IBookmarkRepository>();
    private readonly AddBookmarkCommandHandler _handler;

    public AddBookmarkCommandTests()
    {
        _handler = new AddBookmarkCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldCreateBookmarkAndReturnId()
    {
        var command = new AddBookmarkCommand(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "stream-1", "Orders Stream");

        var outputPort = new TestOutputPort<Guid>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.ShouldNotBe(Guid.Empty);
        await _repository.Received(1).AddAsync(
            Arg.Is<Bookmark>(b => b.ResourceId == "stream-1" && b.DisplayName == "Orders Stream"),
            Arg.Any<CancellationToken>());
    }
}

public sealed class AddBookmarkCommandValidatorTests
{
    private readonly AddBookmarkCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_WhenResourceIdEmpty()
    {
        var command = new AddBookmarkCommand(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "", "Display");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Fail_WhenDisplayNameEmpty()
    {
        var command = new AddBookmarkCommand(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", "");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Fail_WhenDisplayNameTooLong()
    {
        var command = new AddBookmarkCommand(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "id", new string('a', 201));
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new AddBookmarkCommand(Guid.NewGuid(), Guid.NewGuid(), ResourceType.Stream, "stream-1", "Orders");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }
}

public sealed class RemoveBookmarkCommandTests
{
    private readonly IBookmarkRepository _repository = Substitute.For<IBookmarkRepository>();
    private readonly RemoveBookmarkCommandHandler _handler;

    public RemoveBookmarkCommandTests()
    {
        _handler = new RemoveBookmarkCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToRepository()
    {
        var userId = Guid.NewGuid();
        var bookmark = Bookmark.Create(userId, Guid.NewGuid(), ResourceType.Stream, "stream-1", "Orders");
        var bookmarkId = bookmark.Id;
        _repository.GetByIdAsync(bookmarkId, Arg.Any<CancellationToken>()).Returns(bookmark);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new RemoveBookmarkCommand(userId, bookmarkId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).RemoveAsync(bookmarkId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBookmarkBelongsToDifferentUser_ShouldNotDelete()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var bookmark = Bookmark.Create(ownerId, Guid.NewGuid(), ResourceType.Stream, "stream-2", "Invoices");
        var bookmarkId = bookmark.Id;
        _repository.GetByIdAsync(bookmarkId, Arg.Any<CancellationToken>()).Returns(bookmark);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new RemoveBookmarkCommand(actorId, bookmarkId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _repository.DidNotReceive().RemoveAsync(bookmarkId, Arg.Any<CancellationToken>());
    }
}

public sealed class SetPreferenceCommandTests
{
    private readonly IUserPreferenceRepository _repository = Substitute.For<IUserPreferenceRepository>();
    private readonly SetPreferenceCommandHandler _handler;

    public SetPreferenceCommandTests()
    {
        _handler = new SetPreferenceCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotExists_ShouldCreateNewPreference()
    {
        var userId = Guid.NewGuid();
        _repository.GetAsync(userId, "theme", Arg.Any<CancellationToken>()).Returns((UserPreference?)null);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new SetPreferenceCommand(userId, "theme", "dark"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).UpsertAsync(
            Arg.Is<UserPreference>(p => p.Key == "theme" && p.Value == "dark"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenExists_ShouldUpdateExistingPreference()
    {
        var userId = Guid.NewGuid();
        var existing = UserPreference.Create(userId, "theme", "light");
        _repository.GetAsync(userId, "theme", Arg.Any<CancellationToken>()).Returns(existing);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(new SetPreferenceCommand(userId, "theme", "dark"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        existing.Value.ShouldBe("dark");
        await _repository.Received(1).UpsertAsync(existing, Arg.Any<CancellationToken>());
    }
}

public sealed class SetPreferenceCommandValidatorTests
{
    private readonly SetPreferenceCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_WhenKeyEmpty()
    {
        var command = new SetPreferenceCommand(Guid.NewGuid(), "", "value");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Fail_WhenKeyTooLong()
    {
        var command = new SetPreferenceCommand(Guid.NewGuid(), new string('k', 101), "value");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new SetPreferenceCommand(Guid.NewGuid(), "theme", "dark");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }
}

public sealed class GetBookmarksQueryTests
{
    private readonly IBookmarkRepository _repository = Substitute.For<IBookmarkRepository>();
    private readonly GetBookmarksQueryHandler _handler;

    public GetBookmarksQueryTests()
    {
        _handler = new GetBookmarksQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldMapBookmarksToDto()
    {
        var userId = Guid.NewGuid();
        var bookmark = Bookmark.Create(userId, Guid.NewGuid(), ResourceType.Stream, "stream-1", "Orders");
        _repository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(new List<Bookmark> { bookmark });

        var outputPort = new TestOutputPort<IReadOnlyList<BookmarkDto>>();
        await _handler.ExecuteAsync(new GetBookmarksQuery(userId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(1);
        outputPort.Value![0].ResourceId.ShouldBe("stream-1");
        outputPort.Value![0].DisplayName.ShouldBe("Orders");
    }
}

public sealed class SearchQueryTests
{
    private readonly IBookmarkRepository _repository = Substitute.For<IBookmarkRepository>();
    private readonly SearchQueryHandler _handler;

    public SearchQueryTests()
    {
        _handler = new SearchQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldFilterBookmarksByQueryAndEnvironment()
    {
        var userId = Guid.NewGuid();
        var matchingEnvironmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();
        var matching = Bookmark.Create(userId, matchingEnvironmentId, ResourceType.Stream, "orders-stream", "Orders");
        var wrongEnvironment = Bookmark.Create(userId, otherEnvironmentId, ResourceType.Stream, "orders-copy", "Orders Copy");
        var wrongTerm = Bookmark.Create(userId, matchingEnvironmentId, ResourceType.Stream, "invoices", "Invoices");
        _repository.GetByUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Bookmark> { matching, wrongEnvironment, wrongTerm });

        var outputPort = new TestOutputPort<IReadOnlyList<SearchResult>>();
        await _handler.ExecuteAsync(new SearchQuery(userId, "orders", matchingEnvironmentId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        var result = outputPort.Value.ShouldHaveSingleItem();
        result.DisplayName.ShouldBe("Orders");
        result.EnvironmentId.ShouldBe(matchingEnvironmentId);
    }
}

public sealed class GetUserPreferencesQueryTests
{
    private readonly IUserPreferenceRepository _repository = Substitute.For<IUserPreferenceRepository>();
    private readonly GetUserPreferencesQueryHandler _handler;

    public GetUserPreferencesQueryTests()
    {
        _handler = new GetUserPreferencesQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldReturnDictionary()
    {
        var userId = Guid.NewGuid();
        var prefs = new List<UserPreference>
        {
            UserPreference.Create(userId, "theme", "dark"),
            UserPreference.Create(userId, "lang", "en")
        };
        _repository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(prefs);

        var outputPort = new TestOutputPort<Dictionary<string, string>>();
        await _handler.ExecuteAsync(new GetUserPreferencesQuery(userId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(2);
        outputPort.Value!["theme"].ShouldBe("dark");
        outputPort.Value!["lang"].ShouldBe("en");
    }
}

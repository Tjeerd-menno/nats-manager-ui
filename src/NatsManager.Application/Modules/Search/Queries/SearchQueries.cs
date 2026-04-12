using NatsManager.Application.Common;
using NatsManager.Application.Modules.Search.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Search.Queries;

public sealed record SearchResult(
    ResourceType ResourceType,
    string ResourceId,
    string DisplayName,
    Guid? EnvironmentId,
    string? EnvironmentName,
    string? Description);

public sealed record SearchQuery(string Query, Guid? EnvironmentId = null, ResourceType? ResourceType = null);

public sealed record BookmarkDto(Guid Id, Guid UserId, ResourceType ResourceType, string ResourceId, string DisplayName, Guid EnvironmentId, DateTimeOffset CreatedAt);

public sealed record GetBookmarksQuery(Guid UserId);

public sealed class GetBookmarksQueryHandler(IBookmarkRepository repository) : IUseCase<GetBookmarksQuery, IReadOnlyList<BookmarkDto>>
{
    public async Task ExecuteAsync(GetBookmarksQuery request, IOutputPort<IReadOnlyList<BookmarkDto>> outputPort, CancellationToken cancellationToken)
    {
        var bookmarks = await repository.GetByUserAsync(request.UserId, cancellationToken);
        outputPort.Success([.. bookmarks.Select(b => new BookmarkDto(b.Id, b.UserId, b.ResourceType, b.ResourceId, b.DisplayName, b.EnvironmentId, b.CreatedAt))]);
    }
}

public sealed record GetUserPreferencesQuery(Guid UserId);

public sealed class GetUserPreferencesQueryHandler(IUserPreferenceRepository repository) : IUseCase<GetUserPreferencesQuery, Dictionary<string, string>>
{
    public async Task ExecuteAsync(GetUserPreferencesQuery request, IOutputPort<Dictionary<string, string>> outputPort, CancellationToken cancellationToken)
    {
        var prefs = await repository.GetByUserAsync(request.UserId, cancellationToken);
        outputPort.Success(prefs.ToDictionary(p => p.Key, p => p.Value));
    }
}

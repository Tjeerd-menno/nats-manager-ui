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

public sealed record SearchQuery(Guid UserId, string Query, Guid? EnvironmentId = null, ResourceType? ResourceType = null);

public sealed class SearchQueryHandler(IBookmarkRepository repository) : IUseCase<SearchQuery, IReadOnlyList<SearchResult>>
{
    public async Task ExecuteAsync(
        SearchQuery request,
        IOutputPort<IReadOnlyList<SearchResult>> outputPort,
        CancellationToken cancellationToken)
    {
        var term = request.Query.Trim();
        if (term.Length < 2)
        {
            outputPort.Success([]);
            return;
        }

        var bookmarks = await repository.GetByUserAsync(request.UserId, cancellationToken);
        var results = bookmarks
            .Where(bookmark => request.EnvironmentId is null || bookmark.EnvironmentId == request.EnvironmentId)
            .Where(bookmark => request.ResourceType is null || bookmark.ResourceType == request.ResourceType)
            .Where(bookmark =>
                bookmark.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || bookmark.ResourceId.Contains(term, StringComparison.OrdinalIgnoreCase)
                || bookmark.ResourceType.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(bookmark => new SearchResult(
                bookmark.ResourceType,
                bookmark.ResourceId,
                bookmark.DisplayName,
                bookmark.EnvironmentId,
                null,
                null))
            .Take(20)
            .ToList();

        outputPort.Success(results);
    }
}

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

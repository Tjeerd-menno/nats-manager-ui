using NatsManager.Application.Common;
using NatsManager.Application.Modules.Search.Commands;
using NatsManager.Application.Modules.Search.Queries;
using NatsManager.Domain.Modules.Common;
using NatsManager.Web.Presenters;
using System.Security.Claims;

namespace NatsManager.Web.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api")
            .RequireAuthorization();

        group.MapGet("/bookmarks", GetBookmarks);
        group.MapPost("/bookmarks", AddBookmark);
        group.MapDelete("/bookmarks/{bookmarkId:guid}", RemoveBookmark);
        group.MapGet("/preferences", GetPreferences);
        group.MapPut("/preferences/{key}", SetPreference);
    }

    private static async Task<IResult> GetBookmarks(HttpContext httpContext, IUseCase<GetBookmarksQuery, IReadOnlyList<BookmarkDto>> useCase, CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var presenter = new Presenter<IReadOnlyList<BookmarkDto>>();
        await useCase.ExecuteAsync(new GetBookmarksQuery(userId.Value), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> AddBookmark(AddBookmarkRequest request, HttpContext httpContext, IUseCase<AddBookmarkCommand, Guid> useCase, CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var presenter = new Presenter<Guid>();
        await useCase.ExecuteAsync(new AddBookmarkCommand(userId.Value, request.EnvironmentId, request.ResourceType, request.ResourceId, request.DisplayName), presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Created($"/api/bookmarks/{presenter.Value}", new { Id = presenter.Value });
        return presenter.ToResult();
    }

    private static async Task<IResult> RemoveBookmark(Guid bookmarkId, HttpContext httpContext, IUseCase<RemoveBookmarkCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new RemoveBookmarkCommand(userId.Value, bookmarkId), presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> GetPreferences(HttpContext httpContext, IUseCase<GetUserPreferencesQuery, Dictionary<string, string>> useCase, CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var presenter = new Presenter<Dictionary<string, string>>();
        await useCase.ExecuteAsync(new GetUserPreferencesQuery(userId.Value), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> SetPreference(string key, SetPreferenceRequest request, HttpContext httpContext, IUseCase<SetPreferenceCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var userId = GetUserId(httpContext);
        if (userId is null) return Results.Unauthorized();

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new SetPreferenceCommand(userId.Value, key, request.Value), presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        var userIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return userIdStr is not null && Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }
}

public sealed record AddBookmarkRequest(Guid EnvironmentId, ResourceType ResourceType, string ResourceId, string DisplayName);
public sealed record SetPreferenceRequest(string Value);

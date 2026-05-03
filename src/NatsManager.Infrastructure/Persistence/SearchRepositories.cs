using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Modules.Search.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Infrastructure.Persistence;

public sealed class BookmarkRepository(AppDbContext context) : IBookmarkRepository
{
    public async Task<IReadOnlyList<Bookmark>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var bookmarks = await context.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);
        return [.. bookmarks.OrderByDescending(b => b.CreatedAt)];
    }

    public async Task<Bookmark?> GetByIdAsync(Guid bookmarkId, CancellationToken cancellationToken = default)
        => await context.Bookmarks.FirstOrDefaultAsync(b => b.Id == bookmarkId, cancellationToken);

    public async Task AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        context.Bookmarks.Add(bookmark);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid bookmarkId, CancellationToken cancellationToken = default)
    {
        var bookmark = await context.Bookmarks.FirstOrDefaultAsync(b => b.Id == bookmarkId, cancellationToken);
        if (bookmark is not null)
        {
            context.Bookmarks.Remove(bookmark);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class UserPreferenceRepository(AppDbContext context) : IUserPreferenceRepository
{
    public async Task<IReadOnlyList<UserPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserPreferences.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public async Task<UserPreference?> GetAsync(Guid userId, string key, CancellationToken cancellationToken = default)
        => await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.Key == key, cancellationToken);

    public async Task UpsertAsync(UserPreference preference, CancellationToken cancellationToken = default)
    {
        var existing = await context.UserPreferences.FirstOrDefaultAsync(
            p => p.UserId == preference.UserId && p.Key == preference.Key, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateValue(preference.Value);
        }
        else
        {
            context.UserPreferences.Add(preference);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Search.Ports;

public interface IBookmarkRepository
{
    Task<IReadOnlyList<Bookmark>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Bookmark?> GetByIdAsync(Guid bookmarkId, CancellationToken cancellationToken = default);
    Task AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid bookmarkId, CancellationToken cancellationToken = default);
}

public interface IUserPreferenceRepository
{
    Task<IReadOnlyList<UserPreference>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserPreference?> GetAsync(Guid userId, string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(UserPreference preference, CancellationToken cancellationToken = default);
}

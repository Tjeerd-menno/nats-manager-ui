namespace NatsManager.Application.Common;

/// <summary>
/// Helpers for materializing an already-sorted in-memory sequence into a <see cref="PaginatedResult{T}"/>.
/// Centralizes the skip/take and total-count arithmetic shared by the in-memory query handlers.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Pages the source sequence and projects each retained element with <paramref name="selector"/>.
    /// </summary>
    public static PaginatedResult<TResult> ToPaginatedResult<TSource, TResult>(
        this IReadOnlyList<TSource> source,
        int page,
        int pageSize,
        Func<TSource, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var items = source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToList();

        return new PaginatedResult<TResult>
        {
            Items = items,
            TotalCount = source.Count,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>Pages the source sequence without projecting its elements.</summary>
    public static PaginatedResult<T> ToPaginatedResult<T>(
        this IReadOnlyList<T> source,
        int page,
        int pageSize) =>
        source.ToPaginatedResult(page, pageSize, static element => element);
}

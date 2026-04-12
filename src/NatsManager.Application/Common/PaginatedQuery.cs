namespace NatsManager.Application.Common;

public abstract record PaginatedQuery<TResponse>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public string? Search { get; init; }
}

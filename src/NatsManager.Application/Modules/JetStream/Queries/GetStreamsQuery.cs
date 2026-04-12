using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetStreamsQuery : PaginatedQuery<StreamListItem>
{
    public required Guid EnvironmentId { get; init; }
}

public sealed record StreamListItem(
    string Name,
    string Description,
    IReadOnlyList<string> Subjects,
    string RetentionPolicy,
    string StorageType,
    long Messages,
    long Bytes,
    int ConsumerCount,
    DateTimeOffset Created);

public sealed class GetStreamsQueryHandler(
    IJetStreamAdapter jetStreamAdapter) : IUseCase<GetStreamsQuery, PaginatedResult<StreamListItem>>
{
    public async Task ExecuteAsync(GetStreamsQuery request, IOutputPort<PaginatedResult<StreamListItem>> outputPort, CancellationToken cancellationToken)
    {
        var streams = await jetStreamAdapter.ListStreamsAsync(request.EnvironmentId, cancellationToken);

        var filtered = streams.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            filtered = filtered.Where(s =>
                s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "messages" => request.SortDescending ? filtered.OrderByDescending(s => s.Messages) : filtered.OrderBy(s => s.Messages),
            "bytes" => request.SortDescending ? filtered.OrderByDescending(s => s.Bytes) : filtered.OrderBy(s => s.Bytes),
            "consumers" => request.SortDescending ? filtered.OrderByDescending(s => s.ConsumerCount) : filtered.OrderBy(s => s.ConsumerCount),
            "created" => request.SortDescending ? filtered.OrderByDescending(s => s.Created) : filtered.OrderBy(s => s.Created),
            _ => request.SortDescending ? filtered.OrderByDescending(s => s.Name) : filtered.OrderBy(s => s.Name)
        };

        var list = sorted.ToList();
        var totalCount = list.Count;
        var items = list
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new StreamListItem(
                s.Name, s.Description, s.Subjects, s.RetentionPolicy,
                s.StorageType, s.Messages, s.Bytes, s.ConsumerCount, s.Created))
            .ToList();

        outputPort.Success(new PaginatedResult<StreamListItem>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}

using FluentValidation;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetConsumersQuery : PaginatedQuery<ConsumerInfo>
{
    public GetConsumersQuery(Guid environmentId, string streamName)
    {
        EnvironmentId = environmentId;
        StreamName = streamName;
    }

    public Guid EnvironmentId { get; init; }
    public string StreamName { get; init; }
}

public sealed class GetConsumersQueryValidator : AbstractValidator<GetConsumersQuery>
{
    public GetConsumersQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).GreaterThanOrEqualTo(1);
    }
}

public sealed class GetConsumersQueryHandler(
    IJetStreamAdapter jetStreamAdapter) : IUseCase<GetConsumersQuery, PaginatedResult<ConsumerInfo>>
{
    public async Task ExecuteAsync(GetConsumersQuery request, IOutputPort<PaginatedResult<ConsumerInfo>> outputPort, CancellationToken cancellationToken)
    {
        var consumers = await jetStreamAdapter.ListConsumersAsync(request.EnvironmentId, request.StreamName, cancellationToken);

        var filtered = consumers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            filtered = filtered.Where(c =>
                c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (c.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || (c.FilterSubject?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "pending" => request.SortDescending ? filtered.OrderByDescending(c => c.NumPending) : filtered.OrderBy(c => c.NumPending),
            "ackpending" => request.SortDescending ? filtered.OrderByDescending(c => c.NumAckPending) : filtered.OrderBy(c => c.NumAckPending),
            "created" => request.SortDescending ? filtered.OrderByDescending(c => c.Created) : filtered.OrderBy(c => c.Created),
            _ => request.SortDescending ? filtered.OrderByDescending(c => c.Name) : filtered.OrderBy(c => c.Name)
        };

        var list = sorted.ToList();
        var totalCount = list.Count;
        var items = list
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        outputPort.Success(new PaginatedResult<ConsumerInfo>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}

using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Application.Modules.Environments.Queries;

public sealed record GetEnvironmentsQuery : PaginatedQuery<EnvironmentListItem>;

public sealed record EnvironmentListItem(
    Guid Id,
    string Name,
    string Description,
    bool IsEnabled,
    bool IsProduction,
    string ConnectionStatus,
    DateTimeOffset? LastSuccessfulContact);

public sealed class GetEnvironmentsQueryHandler(
    IEnvironmentRepository environmentRepository) : IUseCase<GetEnvironmentsQuery, PaginatedResult<EnvironmentListItem>>
{
    public async Task ExecuteAsync(GetEnvironmentsQuery request, IOutputPort<PaginatedResult<EnvironmentListItem>> outputPort, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await environmentRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        var mapped = items.Select(e => new EnvironmentListItem(
            e.Id,
            e.Name,
            e.Description,
            e.IsEnabled,
            e.IsProduction,
            e.ConnectionStatus.ToString(),
            e.LastSuccessfulContact)).ToList();

        outputPort.Success(new PaginatedResult<EnvironmentListItem>
        {
            Items = mapped,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }
}

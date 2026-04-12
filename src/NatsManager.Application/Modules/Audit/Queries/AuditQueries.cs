using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Audit.Queries;

public sealed record AuditEventDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? ActorId,
    string ActorName,
    ActionType ActionType,
    ResourceType ResourceType,
    string ResourceId,
    string ResourceName,
    Guid? EnvironmentId,
    Outcome Outcome,
    string? Details,
    AuditSource Source);

public sealed record GetAuditEventsQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public Guid? ActorId { get; init; }
    public ActionType? ActionType { get; init; }
    public ResourceType? ResourceType { get; init; }
    public Guid? EnvironmentId { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
    public AuditSource? Source { get; init; }
}

public sealed record AuditEventsResult(IReadOnlyList<AuditEventDto> Items, int TotalCount, int Page, int PageSize);

public sealed class GetAuditEventsQueryHandler(IAuditEventRepository repository) : IUseCase<GetAuditEventsQuery, AuditEventsResult>
{
    public async Task ExecuteAsync(GetAuditEventsQuery request, IOutputPort<AuditEventsResult> outputPort, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.ActorId,
            request.ActionType,
            request.ResourceType,
            request.EnvironmentId,
            request.FromDate,
            request.ToDate,
            request.Source,
            cancellationToken);

        var dtos = items.Select(e => new AuditEventDto(
            e.Id, e.Timestamp, e.ActorId, e.ActorName,
            e.ActionType, e.ResourceType, e.ResourceId, e.ResourceName,
            e.EnvironmentId, e.Outcome, e.Details, e.Source)).ToList();

        outputPort.Success(new AuditEventsResult(dtos, totalCount, request.Page, request.PageSize));
    }
}

using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Audit.Ports;

public interface IAuditEventRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? actorId = null,
        ActionType? actionType = null,
        ResourceType? resourceType = null,
        Guid? environmentId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AuditSource? source = null,
        CancellationToken cancellationToken = default);
}

using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Behaviors;

public interface IAuditableCommand
{
    ActionType ActionType { get; }
    ResourceType ResourceType { get; }
    string ResourceId { get; }
    string ResourceName { get; }
    Guid? EnvironmentId { get; }
}

public interface IAuditContext
{
    Guid? ActorId { get; }
    string ActorName { get; }
}

public interface IAuditTrail
{
    Task RecordAsync(IAuditableCommand command, CancellationToken cancellationToken = default);
    Task RecordAsync(IAuditableCommand command, string resourceIdOverride, CancellationToken cancellationToken = default);
}

public sealed class AuditTrail(IAuditEventRepository auditRepository, IAuditContext auditContext) : IAuditTrail
{
    public Task RecordAsync(IAuditableCommand command, CancellationToken cancellationToken)
        => RecordCoreAsync(command, command.ResourceId, cancellationToken);

    public Task RecordAsync(IAuditableCommand command, string resourceIdOverride, CancellationToken cancellationToken)
        => RecordCoreAsync(command, resourceIdOverride, cancellationToken);

    private async Task RecordCoreAsync(IAuditableCommand command, string resourceId, CancellationToken cancellationToken)
    {
        var auditEvent = AuditEvent.Create(
            actorId: auditContext.ActorId,
            actorName: auditContext.ActorName,
            actionType: command.ActionType,
            resourceType: command.ResourceType,
            resourceId: resourceId,
            resourceName: command.ResourceName,
            environmentId: command.EnvironmentId,
            outcome: Outcome.Success,
            details: null,
            source: AuditSource.UserInitiated);

        await auditRepository.AddAsync(auditEvent, cancellationToken);
    }
}

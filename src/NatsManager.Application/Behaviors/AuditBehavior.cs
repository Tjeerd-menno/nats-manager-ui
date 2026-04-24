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

    /// <summary>
    /// Records an audit event for an action that was not modelled as an <see cref="IAuditableCommand"/>,
    /// for example a failed login attempt where no command object exists but the event still needs an audit trail.
    /// </summary>
    Task RecordAsync(
        ActionType actionType,
        ResourceType resourceType,
        string resourceId,
        string resourceName,
        Guid? environmentId,
        Outcome outcome,
        string? details,
        AuditSource source,
        CancellationToken cancellationToken = default);
}

public sealed class AuditTrail(IAuditEventRepository auditRepository, IAuditContext auditContext) : IAuditTrail
{
    public Task RecordAsync(IAuditableCommand command, CancellationToken cancellationToken)
        => RecordCoreAsync(command, command.ResourceId, cancellationToken);

    public Task RecordAsync(IAuditableCommand command, string resourceIdOverride, CancellationToken cancellationToken)
        => RecordCoreAsync(command, resourceIdOverride, cancellationToken);

    public async Task RecordAsync(
        ActionType actionType,
        ResourceType resourceType,
        string resourceId,
        string resourceName,
        Guid? environmentId,
        Outcome outcome,
        string? details,
        AuditSource source,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = AuditEvent.Create(
            actorId: auditContext.ActorId,
            actorName: auditContext.ActorName,
            actionType: actionType,
            resourceType: resourceType,
            resourceId: resourceId,
            resourceName: resourceName,
            environmentId: environmentId,
            outcome: outcome,
            details: details,
            source: source);

        await auditRepository.AddAsync(auditEvent, cancellationToken);
    }

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

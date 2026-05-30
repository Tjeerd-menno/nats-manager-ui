using Microsoft.Extensions.Logging;
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

public sealed partial class AuditTrail(
    IAuditEventRepository auditRepository,
    IAuditContext auditContext,
    ILogger<AuditTrail> logger) : IAuditTrail
{
    public Task RecordAsync(IAuditableCommand command, CancellationToken cancellationToken)
        => RecordCoreAsync(command, command.ResourceId, cancellationToken);

    public Task RecordAsync(IAuditableCommand command, string resourceIdOverride, CancellationToken cancellationToken)
        => RecordCoreAsync(command, resourceIdOverride, cancellationToken);

    public Task RecordAsync(
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

        return SafePersistAsync(auditEvent, cancellationToken);
    }

    private Task RecordCoreAsync(IAuditableCommand command, string resourceId, CancellationToken cancellationToken)
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

        return SafePersistAsync(auditEvent, cancellationToken);
    }

    /// <summary>
    /// Persists the audit event on a best-effort basis. A failure to write audit
    /// (transient DB error, disk full, etc.) is logged but must never fail the
    /// owning use case — the user action has already succeeded at this point.
    /// Cancellation is honoured so that a cancelled caller still short-circuits.
    /// </summary>
    private async Task SafePersistAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        try
        {
            await auditRepository.AddAsync(auditEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogAuditPersistenceFailed(ex, auditEvent.ActionType, auditEvent.ResourceType, auditEvent.ResourceId);
        }
    }

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Error,
        Message = "Failed to persist audit event for action {ActionType} on {ResourceType} '{ResourceId}'. Audit entry was dropped.")]
    private partial void LogAuditPersistenceFailed(
        Exception exception,
        ActionType actionType,
        ResourceType resourceType,
        string resourceId);
}

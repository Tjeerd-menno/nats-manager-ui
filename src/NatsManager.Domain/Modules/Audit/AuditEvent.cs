using NatsManager.Domain.Modules.Common;

namespace NatsManager.Domain.Modules.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }
    public Guid? ActorId { get; private set; }
    public string ActorName { get; private set; } = string.Empty;
    public ActionType ActionType { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public string ResourceId { get; private set; } = string.Empty;
    public string ResourceName { get; private set; } = string.Empty;
    public Guid? EnvironmentId { get; private set; }
    public Outcome Outcome { get; private set; }
    public string? Details { get; private set; }
    public AuditSource Source { get; private set; }

    private AuditEvent() { }

    public static AuditEvent Create(
        Guid? actorId,
        string actorName,
        ActionType actionType,
        ResourceType resourceType,
        string resourceId,
        string resourceName,
        Guid? environmentId,
        Outcome outcome,
        string? details,
        AuditSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorName);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            ActorId = actorId,
            ActorName = actorName.Trim(),
            ActionType = actionType,
            ResourceType = resourceType,
            ResourceId = resourceId.Trim(),
            ResourceName = resourceName.Trim(),
            EnvironmentId = environmentId,
            Outcome = outcome,
            Details = details,
            Source = source
        };
    }
}

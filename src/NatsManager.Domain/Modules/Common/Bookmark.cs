namespace NatsManager.Domain.Modules.Common;

public sealed class Bookmark
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public string ResourceId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public Guid EnvironmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Bookmark() { }

    public static Bookmark Create(Guid userId, Guid environmentId, ResourceType resourceType, string resourceId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnvironmentId = environmentId,
            ResourceType = resourceType,
            ResourceId = resourceId.Trim(),
            DisplayName = displayName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
    }
}

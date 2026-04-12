namespace NatsManager.Domain.Modules.Common;

public sealed class UserPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserPreference() { }

    public static UserPreference Create(Guid userId, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Key = key.Trim(),
            Value = value ?? string.Empty,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateValue(string value)
    {
        Value = value ?? string.Empty;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

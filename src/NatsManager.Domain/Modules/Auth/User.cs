namespace NatsManager.Domain.Modules.Auth;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { }

    public static User Create(string username, string displayName, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        if (username.Length > 100)
            throw new ArgumentException("Username must not exceed 100 characters.", nameof(username));

        return new User
        {
            Id = Guid.NewGuid(),
            Username = username.Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateProfile(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName.Trim();
    }

    public void UpdatePassword(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

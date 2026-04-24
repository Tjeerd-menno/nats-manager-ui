namespace NatsManager.Domain.Modules.Auth;

public sealed class User
{
    /// <summary>Number of consecutive failed logins that will trigger a temporary lockout.</summary>
    public const int DefaultLockoutThreshold = 5;

    /// <summary>Duration of the temporary lockout after <see cref="DefaultLockoutThreshold"/> failures.</summary>
    public static readonly TimeSpan DefaultLockoutDuration = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

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

    /// <summary>
    /// Marks a successful login. Also resets the failed-attempt counter and clears any lockout.
    /// </summary>
    public void RecordLogin()
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Records a failed login attempt. Once <paramref name="threshold"/> consecutive failures
    /// are reached, the account is temporarily locked for <paramref name="lockoutDuration"/>.
    /// The counter saturates at <paramref name="threshold"/> to avoid integer overflow
    /// from repeated probes against a locked account.
    /// </summary>
    public void RecordFailedLogin(int threshold = DefaultLockoutThreshold, TimeSpan? lockoutDuration = null)
    {
        if (threshold < 1)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be at least 1.");

        if (FailedLoginAttempts < threshold)
        {
            FailedLoginAttempts++;
        }

        if (FailedLoginAttempts >= threshold)
        {
            LockedUntil = DateTimeOffset.UtcNow.Add(lockoutDuration ?? DefaultLockoutDuration);
        }
    }

    /// <summary>
    /// True while an active temporary lockout window is in effect.
    /// </summary>
    public bool IsLocked(DateTimeOffset? now = null)
        => LockedUntil is { } until && until > (now ?? DateTimeOffset.UtcNow);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}

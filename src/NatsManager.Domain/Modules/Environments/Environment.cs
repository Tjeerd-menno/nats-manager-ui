using NatsManager.Domain.Modules.Common;

namespace NatsManager.Domain.Modules.Environments;

public sealed class Environment
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ServerUrl { get; private set; } = string.Empty;
    public CredentialType CredentialType { get; private set; }
    public string CredentialReference { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;
    public bool IsProduction { get; private set; }
    public ConnectionStatus ConnectionStatus { get; private set; } = ConnectionStatus.Unknown;
    public DateTimeOffset? LastSuccessfulContact { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Environment() { }

    public static Environment Create(
        string name,
        string serverUrl,
        string? description = null,
        CredentialType credentialType = CredentialType.None,
        string? credentialReference = null,
        bool isProduction = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);

        if (name.Length > 100)
            throw new ArgumentException("Name must not exceed 100 characters.", nameof(name));

        var now = DateTimeOffset.UtcNow;
        return new Environment
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            ServerUrl = serverUrl.Trim(),
            CredentialType = credentialType,
            CredentialReference = credentialReference ?? string.Empty,
            IsProduction = isProduction,
            IsEnabled = true,
            ConnectionStatus = ConnectionStatus.Unknown,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string serverUrl,
        string? description = null,
        CredentialType credentialType = CredentialType.None,
        string? credentialReference = null,
        bool isProduction = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);

        if (name.Length > 100)
            throw new ArgumentException("Name must not exceed 100 characters.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        ServerUrl = serverUrl.Trim();
        CredentialType = credentialType;
        if (credentialReference is not null)
            CredentialReference = credentialReference;
        IsProduction = isProduction;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateConnectionStatus(ConnectionStatus status)
    {
        ConnectionStatus = status;
        if (status == ConnectionStatus.Available)
            LastSuccessfulContact = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        ConnectionStatus = ConnectionStatus.Unknown;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

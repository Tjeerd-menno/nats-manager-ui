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
    public string? MonitoringUrl { get; private set; }
    public int? MonitoringPollingIntervalSeconds { get; private set; }

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

        ValidateCredentialInvariant(credentialType, credentialReference);

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

        // Work out the final credential reference that will be persisted, then validate the
        // invariant against that. Rules:
        //   * credentialType == None  ⇒ reference is always cleared, regardless of input.
        //   * credentialType != None  ⇒ either a new non-empty reference must be provided,
        //                               or the currently stored reference is reused.
        string finalReference;
        if (credentialType == CredentialType.None)
        {
            finalReference = string.Empty;
        }
        else
        {
            finalReference = credentialReference ?? CredentialReference;
        }

        ValidateCredentialInvariant(credentialType, finalReference);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        ServerUrl = serverUrl.Trim();
        CredentialType = credentialType;
        CredentialReference = finalReference;
        IsProduction = isProduction;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateCredentialInvariant(CredentialType credentialType, string? credentialReference)
    {
        var hasReference = !string.IsNullOrWhiteSpace(credentialReference);

        if (credentialType == CredentialType.None && hasReference)
        {
            throw new ArgumentException(
                "CredentialReference must be empty when CredentialType is None.",
                nameof(credentialReference));
        }

        if (credentialType != CredentialType.None && !hasReference)
        {
            throw new ArgumentException(
                $"CredentialReference is required when CredentialType is {credentialType}.",
                nameof(credentialReference));
        }
    }

    public void UpdateMonitoringSettings(string? monitoringUrl, int? pollingIntervalSeconds)
    {
        if (monitoringUrl is not null)
        {
            if (monitoringUrl.Length > 500)
                throw new ArgumentException("MonitoringUrl must not exceed 500 characters.", nameof(monitoringUrl));
            if (!Uri.TryCreate(monitoringUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                throw new ArgumentException("MonitoringUrl must be a valid http:// or https:// URL.", nameof(monitoringUrl));
        }

        if (pollingIntervalSeconds.HasValue && (pollingIntervalSeconds.Value < 5 || pollingIntervalSeconds.Value > 300))
            throw new ArgumentException("MonitoringPollingIntervalSeconds must be between 5 and 300.", nameof(pollingIntervalSeconds));

        MonitoringUrl = monitoringUrl;
        MonitoringPollingIntervalSeconds = pollingIntervalSeconds;
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

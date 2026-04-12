using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Ports;

public interface IEnvironmentConnectionResolver
{
    Task<EnvironmentConnectionInfo> ResolveAsync(Guid environmentId, CancellationToken cancellationToken = default);
}

public sealed record EnvironmentConnectionInfo(
    string ServerUrl,
    string Name,
    bool IsEnabled,
    CredentialType CredentialType = CredentialType.None,
    string? Credential = null);


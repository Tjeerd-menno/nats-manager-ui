using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Ports;

public interface INatsConnectionFactory
{
    Task<object> GetConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<ConnectionStatus> TestConnectionAsync(string serverUrl, CancellationToken cancellationToken = default);
    Task RemoveConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default);
}

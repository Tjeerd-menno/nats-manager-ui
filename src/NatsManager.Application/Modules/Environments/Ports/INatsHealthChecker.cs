using NatsManager.Application.Modules.Environments.Commands;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Modules.Environments.Ports;

public interface INatsHealthChecker
{
    Task<TestConnectionResult> CheckHealthAsync(Environment environment, CancellationToken cancellationToken = default);
    Task<TestConnectionResult> CheckHealthAsync(string serverUrl, string? credentialReference, CancellationToken cancellationToken = default);
}

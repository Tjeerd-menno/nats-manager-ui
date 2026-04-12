using NatsManager.Application.Modules.Services.Models;

namespace NatsManager.Application.Modules.Services.Ports;

public interface IServiceDiscoveryAdapter
{
    Task<IReadOnlyList<ServiceInfo>> DiscoverServicesAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<ServiceInfo?> GetServiceAsync(Guid environmentId, string serviceName, CancellationToken cancellationToken = default);
    Task<string> TestServiceRequestAsync(Guid environmentId, string subject, string? payload, CancellationToken cancellationToken = default);
}

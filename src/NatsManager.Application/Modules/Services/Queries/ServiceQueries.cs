using NatsManager.Application.Common;
using NatsManager.Application.Modules.Services.Models;
using NatsManager.Application.Modules.Services.Ports;

namespace NatsManager.Application.Modules.Services.Queries;

public sealed record GetServicesQuery(Guid EnvironmentId);

public sealed class GetServicesQueryHandler(IServiceDiscoveryAdapter adapter) : IUseCase<GetServicesQuery, IReadOnlyList<ServiceInfo>>
{
    public async Task ExecuteAsync(GetServicesQuery request, IOutputPort<IReadOnlyList<ServiceInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.DiscoverServicesAsync(request.EnvironmentId, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetServiceDetailQuery(Guid EnvironmentId, string ServiceName);

public sealed class GetServiceDetailQueryHandler(IServiceDiscoveryAdapter adapter) : IUseCase<GetServiceDetailQuery, ServiceInfo>
{
    public async Task ExecuteAsync(GetServiceDetailQuery request, IOutputPort<ServiceInfo> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetServiceAsync(request.EnvironmentId, request.ServiceName, cancellationToken);
        if (result is null) { outputPort.NotFound("Service", request.ServiceName); return; }
        outputPort.Success(result);
    }
}

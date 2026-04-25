using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;

namespace NatsManager.Application.Modules.CoreNats.Queries;

public sealed record GetCoreStatusQuery(Guid EnvironmentId);

public sealed class GetCoreStatusQueryHandler(ICoreNatsAdapter adapter) : IUseCase<GetCoreStatusQuery, NatsServerInfo>
{
    public async Task ExecuteAsync(GetCoreStatusQuery request, IOutputPort<NatsServerInfo> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetServerInfoAsync(request.EnvironmentId, cancellationToken);
        if (result is null) { outputPort.NotFound("NatsServer", request.EnvironmentId.ToString()); return; }
        outputPort.Success(result);
    }
}

public sealed record GetSubjectsQuery(Guid EnvironmentId);

public sealed class GetSubjectsQueryHandler(ICoreNatsAdapter adapter) : IUseCase<GetSubjectsQuery, ListSubjectsResult>
{
    public async Task ExecuteAsync(GetSubjectsQuery request, IOutputPort<ListSubjectsResult> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListSubjectsAsync(request.EnvironmentId, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetClientsQuery(Guid EnvironmentId);

public sealed class GetClientsQueryHandler(ICoreNatsAdapter adapter) : IUseCase<GetClientsQuery, IReadOnlyList<NatsClientInfo>>
{
    public async Task ExecuteAsync(GetClientsQuery request, IOutputPort<IReadOnlyList<NatsClientInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListClientsAsync(request.EnvironmentId, cancellationToken);
        outputPort.Success(result);
    }
}

using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetConsumersQuery(Guid EnvironmentId, string StreamName);

public sealed class GetConsumersQueryHandler(
    IJetStreamAdapter jetStreamAdapter) : IUseCase<GetConsumersQuery, IReadOnlyList<ConsumerInfo>>
{
    public async Task ExecuteAsync(GetConsumersQuery request, IOutputPort<IReadOnlyList<ConsumerInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await jetStreamAdapter.ListConsumersAsync(request.EnvironmentId, request.StreamName, cancellationToken);
        outputPort.Success(result);
    }
}

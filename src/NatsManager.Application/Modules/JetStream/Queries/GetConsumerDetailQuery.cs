using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetConsumerDetailQuery(Guid EnvironmentId, string StreamName, string ConsumerName);

public sealed class GetConsumerDetailQueryHandler(
    IJetStreamAdapter jetStreamAdapter) : IUseCase<GetConsumerDetailQuery, ConsumerInfo>
{
    public async Task ExecuteAsync(GetConsumerDetailQuery request, IOutputPort<ConsumerInfo> outputPort, CancellationToken cancellationToken)
    {
        var consumer = await jetStreamAdapter.GetConsumerAsync(request.EnvironmentId, request.StreamName, request.ConsumerName, cancellationToken);
        if (consumer is null) { outputPort.NotFound("Consumer", request.ConsumerName); return; }
        outputPort.Success(consumer);
    }
}

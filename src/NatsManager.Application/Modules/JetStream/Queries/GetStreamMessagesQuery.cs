using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetStreamMessagesQuery(
    Guid EnvironmentId,
    string StreamName,
    long? StartSequence,
    int Count = 25);

public sealed class GetStreamMessagesQueryHandler(
    IJetStreamAdapter adapter) : IUseCase<GetStreamMessagesQuery, IReadOnlyList<StreamMessage>>
{
    public async Task ExecuteAsync(GetStreamMessagesQuery request, IOutputPort<IReadOnlyList<StreamMessage>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetStreamMessagesAsync(
            request.EnvironmentId,
            request.StreamName,
            request.StartSequence,
            request.Count,
            cancellationToken);
        outputPort.Success(result);
    }
}

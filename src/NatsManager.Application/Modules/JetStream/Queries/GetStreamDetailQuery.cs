using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Modules.JetStream.Queries;

public sealed record GetStreamDetailQuery(Guid EnvironmentId, string StreamName);

public sealed record StreamDetailResult(
    StreamInfo Info,
    StreamConfig Config,
    IReadOnlyList<ConsumerInfo> Consumers);

public sealed class GetStreamDetailQueryHandler(
    IJetStreamAdapter jetStreamAdapter) : IUseCase<GetStreamDetailQuery, StreamDetailResult>
{
    public async Task ExecuteAsync(GetStreamDetailQuery request, IOutputPort<StreamDetailResult> outputPort, CancellationToken cancellationToken)
    {
        var info = await jetStreamAdapter.GetStreamAsync(request.EnvironmentId, request.StreamName, cancellationToken);
        if (info is null) { outputPort.NotFound("Stream", request.StreamName); return; }

        var config = await jetStreamAdapter.GetStreamConfigAsync(request.EnvironmentId, request.StreamName, cancellationToken);
        if (config is null) { outputPort.NotFound("StreamConfig", request.StreamName); return; }

        var consumers = await jetStreamAdapter.ListConsumersAsync(request.EnvironmentId, request.StreamName, cancellationToken);

        outputPort.Success(new StreamDetailResult(info, config, consumers));
    }
}

using NatsManager.Application.Modules.JetStream.Commands;

namespace NatsManager.Application.Modules.JetStream.Ports;

public interface IJetStreamWriteAdapter
{
    Task CreateStreamAsync(CreateStreamCommand command, CancellationToken cancellationToken = default);
    Task UpdateStreamAsync(UpdateStreamCommand command, CancellationToken cancellationToken = default);
    Task DeleteStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default);
    Task PurgeStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default);
    Task CreateConsumerAsync(CreateConsumerCommand command, CancellationToken cancellationToken = default);
    Task DeleteConsumerAsync(Guid environmentId, string streamName, string consumerName, CancellationToken cancellationToken = default);
}

using NatsManager.Application.Modules.JetStream.Models;

namespace NatsManager.Application.Modules.JetStream.Ports;

public interface IJetStreamAdapter
{
    Task<IReadOnlyList<StreamInfo>> ListStreamsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<StreamInfo?> GetStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default);
    Task<StreamConfig?> GetStreamConfigAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsumerInfo>> ListConsumersAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default);
    Task<ConsumerInfo?> GetConsumerAsync(Guid environmentId, string streamName, string consumerName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StreamMessage>> GetStreamMessagesAsync(Guid environmentId, string streamName, long? startSequence, int count, CancellationToken cancellationToken = default);
}

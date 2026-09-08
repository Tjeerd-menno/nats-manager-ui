using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class JetStreamWriteAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<JetStreamWriteAdapter> logger) : IJetStreamWriteAdapter
{
    public async Task CreateStreamAsync(CreateStreamSpec spec, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(spec.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var config = new StreamConfig
        {
            Name = spec.Name,
            Description = spec.Description,
            Subjects = [.. spec.Subjects],
            Retention = Enum.Parse<StreamConfigRetention>(spec.RetentionPolicy, ignoreCase: true),
            Storage = Enum.Parse<StreamConfigStorage>(spec.StorageType, ignoreCase: true),
            MaxMsgs = spec.MaxMessages,
            MaxBytes = spec.MaxBytes,
            NumReplicas = spec.Replicas,
            Discard = Enum.Parse<StreamConfigDiscard>(spec.DiscardPolicy, ignoreCase: true),
        };

        await js.CreateStreamAsync(config, cancellationToken);
        LogStreamCreated(spec.Name, spec.EnvironmentId);
    }

    public async Task UpdateStreamAsync(UpdateStreamSpec spec, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(spec.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var existing = await js.GetStreamAsync(spec.Name, cancellationToken: cancellationToken);
        var config = existing.Info.Config;

        config.Description = spec.Description;
        config.Subjects = [.. spec.Subjects];
        config.MaxMsgs = spec.MaxMessages;
        config.MaxBytes = spec.MaxBytes;
        config.NumReplicas = spec.Replicas;

        await js.UpdateStreamAsync(config, cancellationToken);
        LogStreamUpdated(spec.Name, spec.EnvironmentId);
    }

    public async Task DeleteStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        await js.DeleteStreamAsync(streamName, cancellationToken);
        LogStreamDeleted(streamName, environmentId);
    }

    public async Task PurgeStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        var stream = await js.GetStreamAsync(streamName, cancellationToken: cancellationToken);
        await stream.PurgeAsync(new StreamPurgeRequest(), cancellationToken);
        LogStreamPurged(streamName, environmentId);
    }

    public async Task CreateConsumerAsync(CreateConsumerSpec spec, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(spec.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var config = new ConsumerConfig
        {
            Name = spec.Name,
            Description = spec.Description,
            DurableName = spec.Name,
            DeliverPolicy = Enum.Parse<ConsumerConfigDeliverPolicy>(spec.DeliverPolicy, ignoreCase: true),
            AckPolicy = Enum.Parse<ConsumerConfigAckPolicy>(spec.AckPolicy, ignoreCase: true),
            FilterSubject = spec.FilterSubject,
            MaxDeliver = spec.MaxDeliver,
        };

        await js.CreateOrUpdateConsumerAsync(spec.StreamName, config, cancellationToken);
        LogConsumerCreated(spec.Name, spec.StreamName, spec.EnvironmentId);
    }

    public async Task DeleteConsumerAsync(Guid environmentId, string streamName, string consumerName, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        await js.DeleteConsumerAsync(streamName, consumerName, cancellationToken);
        LogConsumerDeleted(consumerName, streamName, environmentId);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created stream {StreamName} in environment {EnvironmentId}")]
    private partial void LogStreamCreated(string streamName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated stream {StreamName} in environment {EnvironmentId}")]
    private partial void LogStreamUpdated(string streamName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted stream {StreamName} from environment {EnvironmentId}")]
    private partial void LogStreamDeleted(string streamName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged stream {StreamName} in environment {EnvironmentId}")]
    private partial void LogStreamPurged(string streamName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created consumer {ConsumerName} on stream {StreamName} in environment {EnvironmentId}")]
    private partial void LogConsumerCreated(string consumerName, string streamName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted consumer {ConsumerName} from stream {StreamName} in environment {EnvironmentId}")]
    private partial void LogConsumerDeleted(string consumerName, string streamName, Guid environmentId);
}

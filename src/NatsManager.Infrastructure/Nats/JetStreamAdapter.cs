using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Commands;
using NatsManager.Application.Modules.JetStream.Ports;
using AppStreamState = NatsManager.Application.Modules.JetStream.Models.StreamState;
using ConsumerInfo = NatsManager.Application.Modules.JetStream.Models.ConsumerInfo;
using ConsumerState = NatsManager.Application.Modules.JetStream.Models.ConsumerState;
using StreamConfig = NatsManager.Application.Modules.JetStream.Models.StreamConfig;
using StreamInfo = NatsManager.Application.Modules.JetStream.Models.StreamInfo;
using StreamMessage = NatsManager.Application.Modules.JetStream.Models.StreamMessage;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class JetStreamAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<JetStreamAdapter> logger) : IJetStreamAdapter, IJetStreamWriteAdapter
{
    public async Task<IReadOnlyList<StreamInfo>> ListStreamsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        var streams = new List<StreamInfo>();

        await foreach (var stream in js.ListStreamsAsync(cancellationToken: cancellationToken))
        {
            var name = stream.Info.Config.Name ?? string.Empty;
            if (name.StartsWith("KV_", StringComparison.Ordinal) || name.StartsWith("OBJ_", StringComparison.Ordinal))
                continue;

            streams.Add(MapStreamInfo(stream.Info));
        }

        return streams;
    }

    public async Task<StreamInfo?> GetStreamAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
            var js = new NatsJSContext(connection);
            var stream = await js.GetStreamAsync(streamName, cancellationToken: cancellationToken);
            return MapStreamInfo(stream.Info);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            return null;
        }
    }

    public async Task<StreamConfig?> GetStreamConfigAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
            var js = new NatsJSContext(connection);
            var stream = await js.GetStreamAsync(streamName, cancellationToken: cancellationToken);
            return MapStreamConfig(stream.Info.Config);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ConsumerInfo>> ListConsumersAsync(Guid environmentId, string streamName, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        var consumers = new List<ConsumerInfo>();

        await foreach (var consumer in js.ListConsumersAsync(streamName, cancellationToken))
        {
            consumers.Add(MapConsumerInfo(streamName, consumer.Info));
        }

        return consumers;
    }

    public async Task<ConsumerInfo?> GetConsumerAsync(Guid environmentId, string streamName, string consumerName, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
            var js = new NatsJSContext(connection);
            var consumer = await js.GetConsumerAsync(streamName, consumerName, cancellationToken);
            return MapConsumerInfo(streamName, consumer.Info);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            return null;
        }
    }

    private static StreamInfo MapStreamInfo(NATS.Client.JetStream.Models.StreamInfo info)
    {
        return new StreamInfo(
            Name: info.Config.Name ?? string.Empty,
            Description: info.Config.Description ?? string.Empty,
            Subjects: info.Config.Subjects?.ToList() ?? [],
            RetentionPolicy: info.Config.Retention.ToString(),
            StorageType: info.Config.Storage.ToString(),
            Messages: (long)info.State.Messages,
            Bytes: (long)info.State.Bytes,
            ConsumerCount: (int)info.State.ConsumerCount,
            Created: info.Created,
            State: new AppStreamState(
                Messages: (long)info.State.Messages,
                Bytes: (long)info.State.Bytes,
                FirstTimestamp: ParseTimestamp(info.State.FirstTs),
                LastTimestamp: ParseTimestamp(info.State.LastTs),
                FirstSeq: (long)info.State.FirstSeq,
                LastSeq: (long)info.State.LastSeq));
    }

    private static StreamConfig MapStreamConfig(NATS.Client.JetStream.Models.StreamConfig config)
    {
        return new StreamConfig(
            Name: config.Name ?? string.Empty,
            Description: config.Description,
            Subjects: config.Subjects?.ToList() ?? [],
            RetentionPolicy: config.Retention.ToString(),
            MaxMessages: config.MaxMsgs,
            MaxBytes: config.MaxBytes,
            MaxAge: config.MaxAge.Ticks,
            StorageType: config.Storage.ToString(),
            Replicas: config.NumReplicas,
            DiscardPolicy: config.Discard.ToString(),
            MaxMsgSize: config.MaxMsgSize,
            DenyDelete: config.DenyDelete,
            DenyPurge: config.DenyPurge,
            AllowRollup: config.AllowRollupHdrs);
    }

    private static DateTimeOffset? ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return null;
        return DateTimeOffset.TryParse(timestamp, out var result) ? result : null;
    }

    private static ConsumerInfo MapConsumerInfo(string streamName, NATS.Client.JetStream.Models.ConsumerInfo info)
    {
        var pending = (long)info.NumPending;
        var ackPending = info.NumAckPending;
        var redelivered = (long)info.NumRedelivered;

        return new ConsumerInfo(
            StreamName: streamName,
            Name: info.Config.Name ?? info.Name,
            Description: info.Config.Description,
            DeliverPolicy: info.Config.DeliverPolicy.ToString(),
            AckPolicy: info.Config.AckPolicy.ToString(),
            FilterSubject: info.Config.FilterSubject,
            NumPending: pending,
            NumAckPending: ackPending,
            NumRedelivered: redelivered,
            IsHealthy: ackPending < 1000 && redelivered < 100,
            Created: info.Created,
            State: new ConsumerState(
                Delivered: (long)info.Delivered.StreamSeq,
                AckFloor: (long)info.AckFloor.StreamSeq,
                NumPending: pending,
                NumAckPending: ackPending,
                NumRedelivered: redelivered));
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} streams for environment {EnvironmentId}")]
    private partial void LogStreamsListed(int count, Guid environmentId);

    public async Task<IReadOnlyList<StreamMessage>> GetStreamMessagesAsync(Guid environmentId, string streamName, long? startSequence, int count, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        var stream = await js.GetStreamAsync(streamName, cancellationToken: cancellationToken);
        var messages = new List<StreamMessage>();

        // Return empty list for streams with no messages
        if (stream.Info.State.Messages == 0)
            return messages;

        var opts = new NatsJSOrderedConsumerOpts { DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.ByStartSequence };
        var startSeq = startSequence ?? Math.Max(1, (long)stream.Info.State.FirstSeq);

        var consumer = await js.CreateOrderedConsumerAsync(streamName, opts with { OptStartSeq = (ulong)startSeq }, cancellationToken);

        await foreach (var msg in consumer.FetchAsync<byte[]>(new NatsJSFetchOpts { MaxMsgs = count, Expires = TimeSpan.FromSeconds(5) }, cancellationToken: cancellationToken))
        {
            var headers = new Dictionary<string, string>();
            if (msg.Headers is not null)
            {
                foreach (var header in msg.Headers)
                {
                    headers[header.Key] = header.Value.ToString();
                }
            }

            var data = msg.Data is not null ? System.Text.Encoding.UTF8.GetString(msg.Data) : null;

            messages.Add(new StreamMessage(
                Sequence: (long)(msg.Metadata?.Sequence.Stream ?? 0),
                Subject: msg.Subject,
                Data: data,
                Headers: headers,
                Timestamp: msg.Metadata?.Timestamp ?? DateTimeOffset.MinValue,
                Size: msg.Data?.Length ?? 0));

            if (messages.Count >= count)
                break;
        }

        return messages;
    }

    // Write operations

    public async Task CreateStreamAsync(CreateStreamCommand command, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(command.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var config = new NATS.Client.JetStream.Models.StreamConfig
        {
            Name = command.Name,
            Description = command.Description,
            Subjects = [.. command.Subjects],
            Retention = Enum.Parse<NATS.Client.JetStream.Models.StreamConfigRetention>(command.RetentionPolicy, ignoreCase: true),
            Storage = Enum.Parse<NATS.Client.JetStream.Models.StreamConfigStorage>(command.StorageType, ignoreCase: true),
            MaxMsgs = command.MaxMessages,
            MaxBytes = command.MaxBytes,
            NumReplicas = command.Replicas,
            Discard = Enum.Parse<NATS.Client.JetStream.Models.StreamConfigDiscard>(command.DiscardPolicy, ignoreCase: true),
        };

        await js.CreateStreamAsync(config, cancellationToken);
        LogStreamCreated(command.Name, command.EnvironmentId);
    }

    public async Task UpdateStreamAsync(UpdateStreamCommand command, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(command.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var existing = await js.GetStreamAsync(command.Name, cancellationToken: cancellationToken);
        var config = existing.Info.Config;

        config.Description = command.Description;
        config.Subjects = [.. command.Subjects];
        config.MaxMsgs = command.MaxMessages;
        config.MaxBytes = command.MaxBytes;
        config.NumReplicas = command.Replicas;

        await js.UpdateStreamAsync(config, cancellationToken);
        LogStreamUpdated(command.Name, command.EnvironmentId);
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

    public async Task CreateConsumerAsync(CreateConsumerCommand command, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(command.EnvironmentId, cancellationToken);
        var js = new NatsJSContext(connection);

        var config = new NATS.Client.JetStream.Models.ConsumerConfig
        {
            Name = command.Name,
            Description = command.Description,
            DurableName = command.Name,
            DeliverPolicy = Enum.Parse<NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy>(command.DeliverPolicy, ignoreCase: true),
            AckPolicy = Enum.Parse<NATS.Client.JetStream.Models.ConsumerConfigAckPolicy>(command.AckPolicy, ignoreCase: true),
            FilterSubject = command.FilterSubject,
            MaxDeliver = command.MaxDeliver,
        };

        await js.CreateOrUpdateConsumerAsync(command.StreamName, config, cancellationToken);
        LogConsumerCreated(command.Name, command.StreamName, command.EnvironmentId);
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

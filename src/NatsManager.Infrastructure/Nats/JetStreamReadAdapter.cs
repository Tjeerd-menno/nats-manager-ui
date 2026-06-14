using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Ports;
using StreamConfig = NatsManager.Application.Modules.JetStream.Models.StreamConfig;
using StreamInfo = NatsManager.Application.Modules.JetStream.Models.StreamInfo;
using ConsumerInfo = NatsManager.Application.Modules.JetStream.Models.ConsumerInfo;
using StreamMessage = NatsManager.Application.Modules.JetStream.Models.StreamMessage;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class JetStreamReadAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<JetStreamReadAdapter> logger) : IJetStreamAdapter
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

            streams.Add(JetStreamModelMapper.MapStreamInfo(stream.Info));
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
            return JetStreamModelMapper.MapStreamInfo(stream.Info);
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
            return JetStreamModelMapper.MapStreamConfig(stream.Info.Config);
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
            consumers.Add(JetStreamModelMapper.MapConsumerInfo(streamName, consumer.Info));
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
            return JetStreamModelMapper.MapConsumerInfo(streamName, consumer.Info);
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<StreamMessage>> GetStreamMessagesAsync(Guid environmentId, string streamName, long? startSequence, int count, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var js = new NatsJSContext(connection);
        var stream = await js.GetStreamAsync(streamName, cancellationToken: cancellationToken);
        var messages = new List<StreamMessage>();

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed {Count} streams for environment {EnvironmentId}")]
    private partial void LogStreamsListed(int count, Guid environmentId);
}

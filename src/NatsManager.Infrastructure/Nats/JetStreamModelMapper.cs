using NATS.Client.JetStream.Models;
using AppStreamState = NatsManager.Application.Modules.JetStream.Models.StreamState;
using ConsumerInfo = NatsManager.Application.Modules.JetStream.Models.ConsumerInfo;
using ConsumerState = NatsManager.Application.Modules.JetStream.Models.ConsumerState;
using StreamConfig = NatsManager.Application.Modules.JetStream.Models.StreamConfig;
using StreamInfo = NatsManager.Application.Modules.JetStream.Models.StreamInfo;

namespace NatsManager.Infrastructure.Nats;

internal static class JetStreamModelMapper
{
    public static StreamInfo MapStreamInfo(NATS.Client.JetStream.Models.StreamInfo info)
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

    public static StreamConfig MapStreamConfig(NATS.Client.JetStream.Models.StreamConfig config)
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

    public static ConsumerInfo MapConsumerInfo(string streamName, NATS.Client.JetStream.Models.ConsumerInfo info)
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

    private static DateTimeOffset? ParseTimestamp(string? timestamp)
    {
        if (string.IsNullOrEmpty(timestamp))
            return null;

        return DateTimeOffset.TryParse(timestamp, out var result) ? result : null;
    }
}

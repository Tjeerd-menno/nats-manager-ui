namespace NatsManager.Application.Modules.JetStream.Models;

public sealed record StreamInfo(
    string Name,
    string Description,
    IReadOnlyList<string> Subjects,
    string RetentionPolicy,
    string StorageType,
    long Messages,
    long Bytes,
    int ConsumerCount,
    DateTimeOffset Created,
    StreamState State);

public sealed record StreamState(
    long Messages,
    long Bytes,
    DateTimeOffset? FirstTimestamp,
    DateTimeOffset? LastTimestamp,
    long FirstSeq,
    long LastSeq);

public sealed record StreamConfig(
    string Name,
    string? Description,
    IReadOnlyList<string> Subjects,
    string RetentionPolicy,
    long MaxMessages,
    long MaxBytes,
    long MaxAge,
    string StorageType,
    int Replicas,
    string DiscardPolicy,
    int MaxMsgSize,
    bool DenyDelete,
    bool DenyPurge,
    bool AllowRollup);

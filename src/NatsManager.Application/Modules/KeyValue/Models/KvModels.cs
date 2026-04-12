namespace NatsManager.Application.Modules.KeyValue.Models;

public sealed record KvBucketInfo(
    string BucketName,
    int History,
    long MaxBytes,
    int MaxValueSize,
    TimeSpan? Ttl,
    long KeyCount,
    long ByteCount);

public sealed record KvEntry(
    string Key,
    string? Value,
    long Revision,
    string Operation,
    DateTimeOffset CreatedAt,
    int Size);

public sealed record KvKeyHistoryEntry(
    long Revision,
    string Operation,
    DateTimeOffset CreatedAt,
    int Size);

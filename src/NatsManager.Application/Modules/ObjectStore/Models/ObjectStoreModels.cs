namespace NatsManager.Application.Modules.ObjectStore.Models;

public sealed record ObjectBucketInfo(
    string BucketName,
    long ObjectCount,
    long TotalSize,
    string? Description);

public sealed record ObjectInfo(
    string Name,
    long Size,
    string? Description,
    string? ContentType,
    DateTimeOffset? LastModified,
    int Chunks,
    string? Digest);

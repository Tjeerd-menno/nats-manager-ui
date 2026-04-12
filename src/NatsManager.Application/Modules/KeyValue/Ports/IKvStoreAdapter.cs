using NatsManager.Application.Modules.KeyValue.Models;

namespace NatsManager.Application.Modules.KeyValue.Ports;

public interface IKvStoreAdapter
{
    Task<IReadOnlyList<KvBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<KvBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default);
    Task CreateBucketAsync(Guid environmentId, string bucketName, int history, long maxBytes, int maxValueSize, TimeSpan? ttl, CancellationToken cancellationToken = default);
    Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KvEntry>> ListKeysAsync(Guid environmentId, string bucketName, string? search, CancellationToken cancellationToken = default);
    Task<KvEntry?> GetKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KvKeyHistoryEntry>> GetKeyHistoryAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default);
    Task<long> PutKeyAsync(Guid environmentId, string bucketName, string key, byte[] value, long? expectedRevision, CancellationToken cancellationToken = default);
    Task DeleteKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default);
}

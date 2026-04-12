using NatsManager.Application.Modules.ObjectStore.Models;

namespace NatsManager.Application.Modules.ObjectStore.Ports;

public interface IObjectStoreAdapter
{
    Task<IReadOnlyList<ObjectBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<ObjectBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default);
    Task CreateBucketAsync(Guid environmentId, string bucketName, string? description, long? maxBucketSize, long? maxChunkSize, CancellationToken cancellationToken = default);
    Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ObjectInfo>> ListObjectsAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default);
    Task<ObjectInfo?> GetObjectInfoAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default);
    Task UploadObjectAsync(Guid environmentId, string bucketName, string objectName, byte[] data, string? contentType, CancellationToken cancellationToken = default);
    Task DeleteObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default);
}

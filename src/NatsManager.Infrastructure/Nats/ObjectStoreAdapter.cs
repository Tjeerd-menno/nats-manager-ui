using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using NATS.Client.ObjectStore.Models;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Domain.Modules.Common.Errors;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class ObjectStoreAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<ObjectStoreAdapter> logger) : IObjectStoreAdapter
{
    private static readonly TimeSpan ListObjectsTimeout = TimeSpan.FromSeconds(5);

    public async Task<IReadOnlyList<ObjectBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var jsContext = new NatsJSContext(connection);
        var buckets = new List<ObjectBucketInfo>();

        await foreach (var name in jsContext.ListStreamNamesAsync(cancellationToken: cancellationToken))
        {
            if (!name.StartsWith("OBJ_", StringComparison.Ordinal))
                continue;

            var bucketName = name[4..];
            try
            {
                var context = new NatsObjContext(jsContext);
                var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
                var status = await store.GetStatusAsync(cancellationToken);
                buckets.Add(new ObjectBucketInfo(
                    BucketName: bucketName,
                    ObjectCount: (long)status.Info.State.Messages,
                    TotalSize: (long)status.Info.State.Bytes,
                    Description: status.Info.Config.Description));
            }
            catch
            {
            }
        }

        return buckets;
    }

    public async Task<ObjectBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = await GetObjContextAsync(environmentId, cancellationToken);
            var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
            var status = await store.GetStatusAsync(cancellationToken);
            return new ObjectBucketInfo(
                BucketName: status.Bucket,
                ObjectCount: (long)status.Info.State.Messages,
                TotalSize: (long)status.Info.State.Bytes,
                Description: status.Info.Config.Description);
        }
        catch
        {
            return null;
        }
    }

    public async Task CreateBucketAsync(Guid environmentId, string bucketName, string? description, long? maxBucketSize, long? maxChunkSize, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        var config = new NatsObjConfig(bucketName)
        {
            Description = description,
            MaxBytes = maxBucketSize ?? -1,
        };

        try
        {
            await context.CreateObjectStoreAsync(config, cancellationToken);
        }
        catch (NatsJSApiException ex) when (ex.Error.Description?.Contains("already in use", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new ConflictException($"Object store bucket '{bucketName}' already exists.");
        }

        LogBucketCreated(bucketName, environmentId);
    }

    public async Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        await context.DeleteObjectStore(bucketName, cancellationToken);
        LogBucketDeleted(bucketName, environmentId);
    }

    public async Task<IReadOnlyList<ObjectInfo>> ListObjectsAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
        var objects = new List<ObjectInfo>();

        using var timeoutCts = new CancellationTokenSource(ListObjectsTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await foreach (var info in store.ListAsync(cancellationToken: linkedCts.Token))
            {
                objects.Add(MapObjectInfo(info));
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
        }

        return objects;
    }

    public async Task<ObjectInfo?> GetObjectInfoAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = await GetObjContextAsync(environmentId, cancellationToken);
            var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
            var info = await store.GetInfoAsync(objectName, cancellationToken: cancellationToken);
            return MapObjectInfo(info);
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]> DownloadObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
        using var ms = new MemoryStream();
        await store.GetAsync(objectName, ms, cancellationToken: cancellationToken);
        return ms.ToArray();
    }

    public async Task UploadObjectAsync(Guid environmentId, string bucketName, string objectName, byte[] data, string? contentType, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
        using var ms = new MemoryStream(data);
        var meta = new ObjectMetadata { Name = objectName };
        if (contentType is not null)
        {
            meta.Headers = new Dictionary<string, string[]> { ["Content-Type"] = [contentType] };
        }
        await store.PutAsync(meta, ms, cancellationToken: cancellationToken);
        LogObjectUploaded(objectName, bucketName, environmentId);
    }

    public async Task DeleteObjectAsync(Guid environmentId, string bucketName, string objectName, CancellationToken cancellationToken = default)
    {
        var context = await GetObjContextAsync(environmentId, cancellationToken);
        var store = await context.GetObjectStoreAsync(bucketName, cancellationToken);
        await store.DeleteAsync(objectName, cancellationToken);
        LogObjectDeleted(objectName, bucketName, environmentId);
    }

    private async Task<INatsObjContext> GetObjContextAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var jsContext = new NatsJSContext(connection);
        return new NatsObjContext(jsContext);
    }

    private static ObjectInfo MapObjectInfo(ObjectMetadata info) =>
        new(
            Name: info.Name,
            Size: (long)info.Size,
            Description: info.Description,
            ContentType: info.Headers?.TryGetValue("Content-Type", out var ct) == true ? string.Join(", ", ct) : null,
            LastModified: info.MTime != default ? info.MTime : null,
            Chunks: (int)info.Chunks,
            Digest: info.Digest);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created object bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogBucketCreated(string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted object bucket {BucketName} from environment {EnvironmentId}")]
    private partial void LogBucketDeleted(string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Uploaded object {ObjectName} to bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogObjectUploaded(string objectName, string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted object {ObjectName} from bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogObjectDeleted(string objectName, string bucketName, Guid environmentId);
}

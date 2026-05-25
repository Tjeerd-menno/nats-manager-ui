using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Domain.Modules.Common.Errors;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class KvStoreAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<KvStoreAdapter> logger) : IKvStoreAdapter
{
    public async Task<IReadOnlyList<KvBucketInfo>> ListBucketsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var buckets = new List<KvBucketInfo>();

        await foreach (var status in context.GetStatusesAsync(cancellationToken))
        {
            if (!TryGetExternalBucketName(status.Bucket, status.Info.Config.Subjects, out var bucketName))
                continue;

            try
            {
                buckets.Add(MapBucketInfo(status, bucketName));
            }
            catch (Exception ex)
            {
                LogBucketError(status.Bucket, environmentId, ex);
            }
        }

        return buckets;
    }

    public async Task<KvBucketInfo?> GetBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = await GetKvContextAsync(environmentId, cancellationToken);
            var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);
            var status = await store.GetStatusAsync(cancellationToken);
            return MapBucketInfo(status, bucketName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task CreateBucketAsync(Guid environmentId, string bucketName, int history, long maxBytes, int maxValueSize, TimeSpan? ttl, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var config = new NatsKVConfig(bucketName)
        {
            History = history,
            MaxBytes = maxBytes,
            MaxValueSize = maxValueSize > 0 ? maxValueSize : 0,
        };

        if (ttl.HasValue)
        {
            config = config with { MaxAge = ttl.Value };
        }

        await context.CreateStoreAsync(config, cancellationToken);
        LogBucketCreated(bucketName, environmentId);
    }

    public async Task DeleteBucketAsync(Guid environmentId, string bucketName, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        await context.DeleteStoreAsync(bucketName, cancellationToken);
        LogBucketDeleted(bucketName, environmentId);
    }

    public async Task<IReadOnlyList<KvEntry>> ListKeysAsync(Guid environmentId, string bucketName, string? search, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);
        var entries = new List<KvEntry>();

        await foreach (var key in store.GetKeysAsync(cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(search) && !key.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var entry = await store.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
                entries.Add(MapKvEntry(entry));
            }
            catch (NatsKVKeyNotFoundException)
            {
                // Key was deleted between listing and retrieval
            }
        }

        return entries;
    }

    public async Task<KvEntry?> GetKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = await GetKvContextAsync(environmentId, cancellationToken);
            var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);
            var entry = await store.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
            return MapKvEntry(entry);
        }
        catch (NatsKVKeyNotFoundException)
        {
            return null;
        }
        catch (NatsKVKeyDeletedException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<KvKeyHistoryEntry>> GetKeyHistoryAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);
        var history = new List<KvKeyHistoryEntry>();

        await foreach (var entry in store.HistoryAsync<byte[]>(key, cancellationToken: cancellationToken))
        {
            history.Add(new KvKeyHistoryEntry(
                Revision: (long)entry.Revision,
                Operation: entry.Operation.ToString(),
                CreatedAt: entry.Created,
                Size: entry.Value?.Length ?? 0));
        }

        return history;
    }

    public async Task<long> PutKeyAsync(Guid environmentId, string bucketName, string key, byte[] value, long? expectedRevision, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);

        try
        {
            var revision = expectedRevision.HasValue
                ? await store.UpdateAsync(key, value, (ulong)expectedRevision.Value, cancellationToken: cancellationToken)
                : await store.PutAsync(key, value, cancellationToken: cancellationToken);

            LogKeyUpdated(key, bucketName, environmentId);
            return (long)revision;
        }
        catch (NatsKVWrongLastRevisionException)
        {
            throw new ConflictException($"Key '{key}' has been modified. Expected revision {expectedRevision} is no longer current.");
        }
    }

    public async Task DeleteKeyAsync(Guid environmentId, string bucketName, string key, CancellationToken cancellationToken = default)
    {
        var context = await GetKvContextAsync(environmentId, cancellationToken);
        var store = await context.GetStoreAsync(bucketName, cancellationToken: cancellationToken);
        await store.DeleteAsync(key, cancellationToken: cancellationToken);
        LogKeyDeleted(key, bucketName, environmentId);
    }

    private async Task<INatsKVContext> GetKvContextAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var jsContext = new NatsJSContext(connection);
        return new NatsKVContext(jsContext);
    }

    internal static bool TryGetExternalBucketName(string statusBucket, IEnumerable<string>? subjects, out string bucketName)
    {
        bucketName = string.Empty;

        if (string.IsNullOrWhiteSpace(statusBucket)
            || subjects is null
            || !subjects.Any(subject => subject.StartsWith("$KV.", StringComparison.Ordinal)))
            return false;

        bucketName = statusBucket.StartsWith("KV_", StringComparison.Ordinal) ? statusBucket[3..] : statusBucket;
        return !string.IsNullOrWhiteSpace(bucketName);
    }

    private static KvBucketInfo MapBucketInfo(NatsKVStatus status, string bucketName)
    {
        var config = status.Info.Config;
        var state = status.Info.State;
        return new KvBucketInfo(
            BucketName: bucketName,
            History: (int)config.MaxMsgsPerSubject,
            MaxBytes: config.MaxBytes,
            MaxValueSize: (int)config.MaxMsgSize,
            Ttl: config.MaxAge > TimeSpan.Zero ? config.MaxAge : null,
            KeyCount: (long)state.NumSubjects,
            ByteCount: (long)state.Bytes);
    }

    private static KvEntry MapKvEntry(NatsKVEntry<byte[]> entry)
    {
        var valueBase64 = entry.Value is not null ? Convert.ToBase64String(entry.Value) : null;
        return new KvEntry(
            Key: entry.Key,
            Value: valueBase64,
            Revision: (long)entry.Revision,
            Operation: entry.Operation.ToString(),
            CreatedAt: entry.Created,
            Size: entry.Value?.Length ?? 0);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error listing bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogBucketError(string bucketName, Guid environmentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created KV bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogBucketCreated(string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted KV bucket {BucketName} from environment {EnvironmentId}")]
    private partial void LogBucketDeleted(string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated key {Key} in bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogKeyUpdated(string key, string bucketName, Guid environmentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Deleted key {Key} from bucket {BucketName} in environment {EnvironmentId}")]
    private partial void LogKeyDeleted(string key, string bucketName, Guid environmentId);
}

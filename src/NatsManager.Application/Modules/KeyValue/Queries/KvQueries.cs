using NatsManager.Application.Common;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Ports;

namespace NatsManager.Application.Modules.KeyValue.Queries;

public sealed record GetKvBucketsQuery(Guid EnvironmentId);

public sealed class GetKvBucketsQueryHandler(IKvStoreAdapter adapter) : IUseCase<GetKvBucketsQuery, IReadOnlyList<KvBucketInfo>>
{
    public async Task ExecuteAsync(GetKvBucketsQuery request, IOutputPort<IReadOnlyList<KvBucketInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListBucketsAsync(request.EnvironmentId, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetKvBucketDetailQuery(Guid EnvironmentId, string BucketName);

public sealed class GetKvBucketDetailQueryHandler(IKvStoreAdapter adapter) : IUseCase<GetKvBucketDetailQuery, KvBucketInfo>
{
    public async Task ExecuteAsync(GetKvBucketDetailQuery request, IOutputPort<KvBucketInfo> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetBucketAsync(request.EnvironmentId, request.BucketName, cancellationToken);
        if (result is null) { outputPort.NotFound("KvBucket", request.BucketName); return; }
        outputPort.Success(result);
    }
}

public sealed record GetKvKeysQuery(Guid EnvironmentId, string BucketName, string? Search);

public sealed class GetKvKeysQueryHandler(IKvStoreAdapter adapter) : IUseCase<GetKvKeysQuery, IReadOnlyList<KvEntry>>
{
    public async Task ExecuteAsync(GetKvKeysQuery request, IOutputPort<IReadOnlyList<KvEntry>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListKeysAsync(request.EnvironmentId, request.BucketName, request.Search, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetKvKeyDetailQuery(Guid EnvironmentId, string BucketName, string Key);

public sealed class GetKvKeyDetailQueryHandler(IKvStoreAdapter adapter) : IUseCase<GetKvKeyDetailQuery, KvEntry>
{
    public async Task ExecuteAsync(GetKvKeyDetailQuery request, IOutputPort<KvEntry> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetKeyAsync(request.EnvironmentId, request.BucketName, request.Key, cancellationToken);
        if (result is null) { outputPort.NotFound("KvKey", request.Key); return; }
        outputPort.Success(result);
    }
}

public sealed record GetKvKeyHistoryQuery(Guid EnvironmentId, string BucketName, string Key);

public sealed class GetKvKeyHistoryQueryHandler(IKvStoreAdapter adapter) : IUseCase<GetKvKeyHistoryQuery, IReadOnlyList<KvKeyHistoryEntry>>
{
    public async Task ExecuteAsync(GetKvKeyHistoryQuery request, IOutputPort<IReadOnlyList<KvKeyHistoryEntry>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetKeyHistoryAsync(request.EnvironmentId, request.BucketName, request.Key, cancellationToken);
        outputPort.Success(result);
    }
}

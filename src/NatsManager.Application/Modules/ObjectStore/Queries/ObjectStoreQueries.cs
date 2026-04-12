using NatsManager.Application.Common;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Ports;

namespace NatsManager.Application.Modules.ObjectStore.Queries;

public sealed record GetObjectBucketsQuery(Guid EnvironmentId);

public sealed class GetObjectBucketsQueryHandler(IObjectStoreAdapter adapter) : IUseCase<GetObjectBucketsQuery, IReadOnlyList<ObjectBucketInfo>>
{
    public async Task ExecuteAsync(GetObjectBucketsQuery request, IOutputPort<IReadOnlyList<ObjectBucketInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListBucketsAsync(request.EnvironmentId, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetObjectBucketDetailQuery(Guid EnvironmentId, string BucketName);

public sealed class GetObjectBucketDetailQueryHandler(IObjectStoreAdapter adapter) : IUseCase<GetObjectBucketDetailQuery, ObjectBucketInfo>
{
    public async Task ExecuteAsync(GetObjectBucketDetailQuery request, IOutputPort<ObjectBucketInfo> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetBucketAsync(request.EnvironmentId, request.BucketName, cancellationToken);
        if (result is null) { outputPort.NotFound("ObjectBucket", request.BucketName); return; }
        outputPort.Success(result);
    }
}

public sealed record GetObjectsQuery(Guid EnvironmentId, string BucketName);

public sealed class GetObjectsQueryHandler(IObjectStoreAdapter adapter) : IUseCase<GetObjectsQuery, IReadOnlyList<ObjectInfo>>
{
    public async Task ExecuteAsync(GetObjectsQuery request, IOutputPort<IReadOnlyList<ObjectInfo>> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.ListObjectsAsync(request.EnvironmentId, request.BucketName, cancellationToken);
        outputPort.Success(result);
    }
}

public sealed record GetObjectDetailQuery(Guid EnvironmentId, string BucketName, string ObjectName);

public sealed class GetObjectDetailQueryHandler(IObjectStoreAdapter adapter) : IUseCase<GetObjectDetailQuery, ObjectInfo>
{
    public async Task ExecuteAsync(GetObjectDetailQuery request, IOutputPort<ObjectInfo> outputPort, CancellationToken cancellationToken)
    {
        var result = await adapter.GetObjectInfoAsync(request.EnvironmentId, request.BucketName, request.ObjectName, cancellationToken);
        if (result is null) { outputPort.NotFound("Object", request.ObjectName); return; }
        outputPort.Success(result);
    }
}

public sealed record DownloadObjectQuery(Guid EnvironmentId, string BucketName, string ObjectName);

public sealed class DownloadObjectQueryHandler(IObjectStoreAdapter adapter) : IUseCase<DownloadObjectQuery, byte[]?>
{
    public async Task ExecuteAsync(DownloadObjectQuery request, IOutputPort<byte[]?> outputPort, CancellationToken cancellationToken)
    {
        try
        {
            var result = await adapter.DownloadObjectAsync(request.EnvironmentId, request.BucketName, request.ObjectName, cancellationToken);
            if (result is null) { outputPort.NotFound("Object", request.ObjectName); return; }
            outputPort.Success(result);
        }
        catch
        {
            outputPort.NotFound("Object", request.ObjectName);
        }
    }
}

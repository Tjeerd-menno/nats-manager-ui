using NatsManager.Application.Common;
using NatsManager.Application.Modules.ObjectStore.Commands;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Queries;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class ObjectStoreEndpoints
{
    public static IEndpointRouteBuilder MapObjectStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/objectstore")
            .WithTags("Object Store")
            .RequireAuthorization();

        group.MapGet("/buckets", GetBuckets);
        group.MapGet("/buckets/{bucket}", GetBucketDetail);
        group.MapPost("/buckets", CreateBucket).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/buckets/{bucket}", DeleteBucket).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapGet("/buckets/{bucket}/objects", GetObjects);
        group.MapGet("/buckets/{bucket}/objects/{objectName}", GetObjectDetail);
        group.MapGet("/buckets/{bucket}/objects/{objectName}/download", DownloadObject);
        group.MapPost("/buckets/{bucket}/objects/{objectName}/upload", UploadObject)
            .RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/buckets/{bucket}/objects/{objectName}", DeleteObject).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);

        return app;
    }

    private static async Task<IResult> GetBuckets(Guid envId, IUseCase<GetObjectBucketsQuery, IReadOnlyList<ObjectBucketInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<ObjectBucketInfo>>();
        await useCase.ExecuteAsync(new GetObjectBucketsQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetBucketDetail(Guid envId, string bucket, IUseCase<GetObjectBucketDetailQuery, ObjectBucketInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ObjectBucketInfo>();
        await useCase.ExecuteAsync(new GetObjectBucketDetailQuery(envId, bucket), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> CreateBucket(Guid envId, CreateObjectBucketRequest request, IUseCase<CreateObjectBucketCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new CreateObjectBucketCommand
        {
            EnvironmentId = envId,
            BucketName = request.BucketName,
            Description = request.Description,
            MaxBucketSize = request.MaxBucketSize,
            MaxChunkSize = request.MaxChunkSize,
        }, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{envId}/objectstore/buckets/{request.BucketName}");
    }

    private static async Task<IResult> DeleteBucket(
        Guid envId, string bucket,
        HttpContext httpContext,
        IUseCase<DeleteObjectBucketCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteObjectBucketCommand { EnvironmentId = envId, BucketName = bucket }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> GetObjects(Guid envId, string bucket, IUseCase<GetObjectsQuery, IReadOnlyList<ObjectInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<ObjectInfo>>();
        await useCase.ExecuteAsync(new GetObjectsQuery(envId, bucket), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetObjectDetail(Guid envId, string bucket, string objectName, IUseCase<GetObjectDetailQuery, ObjectInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ObjectInfo>();
        await useCase.ExecuteAsync(new GetObjectDetailQuery(envId, bucket, objectName), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> DownloadObject(Guid envId, string bucket, string objectName, IUseCase<DownloadObjectQuery, byte[]?> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<byte[]?>();
        await useCase.ExecuteAsync(new DownloadObjectQuery(envId, bucket, objectName), presenter, cancellationToken);
        if (presenter.IsSuccess && presenter.Value is not null)
            return Results.File(presenter.Value, "application/octet-stream", objectName);
        return presenter.ToResult();
    }

    private static async Task<IResult> UploadObject(
        Guid envId, string bucket, string objectName,
        HttpRequest httpRequest,
        IUseCase<UploadObjectCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await httpRequest.Body.CopyToAsync(ms, cancellationToken);
        var data = ms.ToArray();
        var contentType = httpRequest.ContentType;

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new UploadObjectCommand
        {
            EnvironmentId = envId,
            BucketName = bucket,
            ObjectName = objectName,
            Data = data,
            ContentType = contentType,
        }, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{envId}/objectstore/buckets/{bucket}/objects/{objectName}");
    }

    private static async Task<IResult> DeleteObject(
        Guid envId, string bucket, string objectName,
        HttpContext httpContext,
        IUseCase<DeleteObjectCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteObjectCommand { EnvironmentId = envId, BucketName = bucket, ObjectName = objectName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }
}

public sealed record CreateObjectBucketRequest(string BucketName, string? Description, long? MaxBucketSize, long? MaxChunkSize);

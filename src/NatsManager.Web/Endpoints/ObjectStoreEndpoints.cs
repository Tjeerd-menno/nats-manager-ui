using System.Buffers;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.ObjectStore.Commands;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Queries;
using NatsManager.Web.Configuration;
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
        return presenter.IsSuccess ? Results.Ok(new ListResponse<ObjectBucketInfo>(presenter.Value!)) : presenter.ToResult();
    }

    private static async Task<IResult> GetBucketDetail(Guid envId, string bucket, IUseCase<GetObjectBucketDetailQuery, ObjectBucketInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ObjectBucketInfo>();
        await useCase.ExecuteAsync(new GetObjectBucketDetailQuery(envId, bucket), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> CreateBucket(
        Guid envId,
        CreateObjectBucketRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<CreateObjectBucketCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

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
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteObjectBucketCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

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
        return presenter.IsSuccess ? Results.Ok(new ListResponse<ObjectInfo>(presenter.Value!)) : presenter.ToResult();
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
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IOptions<ObjectStoreUploadOptions> uploadOptions,
        IUseCase<UploadObjectCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var data = await ReadBoundedBodyAsync(httpRequest, uploadOptions.Value.MaxUploadBytes, cancellationToken);
        if (data is null)
        {
            return Results.Problem(
                title: "Object upload too large",
                detail: $"Object uploads are limited to {uploadOptions.Value.MaxUploadBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

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
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteObjectCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteObjectCommand { EnvironmentId = envId, BucketName = bucket, ObjectName = objectName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<byte[]?> ReadBoundedBodyAsync(HttpRequest request, long maxBytes, CancellationToken cancellationToken)
    {
        if (request.ContentLength is > 0 and var contentLength)
        {
            if (contentLength > maxBytes)
            {
                return null;
            }

            return await ReadKnownLengthBodyAsync(request.Body, (int)contentLength, cancellationToken);
        }

        return await ReadUnknownLengthBodyAsync(request.Body, maxBytes, cancellationToken);
    }

    private static async Task<byte[]> ReadKnownLengthBodyAsync(Stream body, int contentLength, CancellationToken cancellationToken)
    {
        var data = new byte[contentLength];
        var totalRead = 0;

        while (totalRead < data.Length)
        {
            var read = await body.ReadAsync(data.AsMemory(totalRead), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (totalRead == data.Length)
        {
            return data;
        }

        Array.Resize(ref data, totalRead);
        return data;
    }

    private static async Task<byte[]?> ReadUnknownLengthBodyAsync(Stream body, long maxBytes, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        var totalRead = 0L;

        try
        {
            while (true)
            {
                var read = await body.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    return ms.ToArray();
                }

                totalRead += read;
                if (totalRead > maxBytes)
                {
                    return null;
                }

                ms.Write(buffer.AsSpan(0, read));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

public sealed record CreateObjectBucketRequest(string BucketName, string? Description, long? MaxBucketSize, long? MaxChunkSize);

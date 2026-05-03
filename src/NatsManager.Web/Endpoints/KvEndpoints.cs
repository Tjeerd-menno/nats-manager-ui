using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.KeyValue.Commands;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Queries;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class KvEndpoints
{
    public static IEndpointRouteBuilder MapKvEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/kv")
            .WithTags("Key-Value Store")
            .RequireAuthorization();

        group.MapGet("/buckets", GetBuckets);
        group.MapGet("/buckets/{bucket}", GetBucketDetail);
        group.MapPost("/buckets", CreateBucket).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/buckets/{bucket}", DeleteBucket).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapGet("/buckets/{bucket}/keys", GetKeys);
        group.MapGet("/buckets/{bucket}/keys/{key}", GetKeyDetail);
        group.MapGet("/buckets/{bucket}/keys/{key}/history", GetKeyHistory);
        group.MapPut("/buckets/{bucket}/keys/{key}", PutKey).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/buckets/{bucket}/keys/{key}", DeleteKey).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);

        return app;
    }

    private static async Task<IResult> GetBuckets(Guid envId, IUseCase<GetKvBucketsQuery, IReadOnlyList<KvBucketInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<KvBucketInfo>>();
        await useCase.ExecuteAsync(new GetKvBucketsQuery(envId), presenter, cancellationToken);
        return presenter.IsSuccess ? Results.Ok(new ListResponse<KvBucketInfo>(presenter.Value!)) : presenter.ToResult();
    }

    private static async Task<IResult> GetBucketDetail(Guid envId, string bucket, IUseCase<GetKvBucketDetailQuery, KvBucketInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<KvBucketInfo>();
        await useCase.ExecuteAsync(new GetKvBucketDetailQuery(envId, bucket), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> CreateBucket(
        Guid envId,
        CreateKvBucketRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<CreateKvBucketCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new CreateKvBucketCommand
        {
            EnvironmentId = envId,
            BucketName = request.BucketName,
            History = request.History ?? 1,
            MaxBytes = request.MaxBytes ?? -1,
            MaxValueSize = request.MaxValueSize ?? -1,
            Ttl = request.Ttl.HasValue ? TimeSpan.FromSeconds(request.Ttl.Value) : null,
        }, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{envId}/kv/buckets/{request.BucketName}");
    }

    private static async Task<IResult> DeleteBucket(
        Guid envId, string bucket,
        HttpContext httpContext,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteKvBucketCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return ApiProblemResults.ConfirmationRequired("X-Confirm header must be 'true' for destructive operations.");

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteKvBucketCommand { EnvironmentId = envId, BucketName = bucket }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> GetKeys(
        Guid envId, string bucket,
        [FromQuery] string? search,
        IUseCase<GetKvKeysQuery, IReadOnlyList<KvEntry>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<KvEntry>>();
        await useCase.ExecuteAsync(new GetKvKeysQuery(envId, bucket, search), presenter, cancellationToken);
        return presenter.IsSuccess ? Results.Ok(new ListResponse<KvEntry>(presenter.Value!)) : presenter.ToResult();
    }

    private static async Task<IResult> GetKeyDetail(Guid envId, string bucket, string key, IUseCase<GetKvKeyDetailQuery, KvEntry> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<KvEntry>();
        await useCase.ExecuteAsync(new GetKvKeyDetailQuery(envId, bucket, key), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetKeyHistory(Guid envId, string bucket, string key, IUseCase<GetKvKeyHistoryQuery, IReadOnlyList<KvKeyHistoryEntry>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<KvKeyHistoryEntry>>();
        await useCase.ExecuteAsync(new GetKvKeyHistoryQuery(envId, bucket, key), presenter, cancellationToken);
        return presenter.IsSuccess ? Results.Ok(new ListResponse<KvKeyHistoryEntry>(presenter.Value!)) : presenter.ToResult();
    }

    private static async Task<IResult> PutKey(
        Guid envId,
        string bucket,
        string key,
        PutKvKeyRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<PutKvKeyCommand, long> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var presenter = new Presenter<long>();
        await useCase.ExecuteAsync(new PutKvKeyCommand
        {
            EnvironmentId = envId,
            BucketName = bucket,
            Key = key,
            Value = request.Value,
            ExpectedRevision = request.ExpectedRevision,
        }, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Ok(new { revision = presenter.Value });
        return presenter.ToResult();
    }

    private static async Task<IResult> DeleteKey(
        Guid envId, string bucket, string key,
        HttpContext httpContext,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteKvKeyCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return ApiProblemResults.ConfirmationRequired("X-Confirm header must be 'true' for destructive operations.");

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteKvKeyCommand { EnvironmentId = envId, BucketName = bucket, Key = key }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }
}

public sealed record CreateKvBucketRequest(string BucketName, int? History, long? MaxBytes, int? MaxValueSize, long? Ttl);
public sealed record PutKvKeyRequest(string Value, long? ExpectedRevision);

using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Commands;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Queries;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class JetStreamEndpoints
{
    public static IEndpointRouteBuilder MapJetStreamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/jetstream")
            .WithTags("JetStream")
            .RequireAuthorization();

        group.MapGet("/streams", GetStreams);
        group.MapGet("/streams/{streamName}", GetStreamDetail);
        group.MapGet("/streams/{streamName}/consumers", GetConsumers);
        group.MapGet("/streams/{streamName}/consumers/{consumerName}", GetConsumerDetail);
        group.MapGet("/streams/{streamName}/messages", GetStreamMessages);

        group.MapPost("/streams", CreateStream).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPut("/streams/{streamName}", UpdateStream).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/streams/{streamName}", DeleteStream).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPost("/streams/{streamName}/purge", PurgeStream).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapPost("/streams/{streamName}/consumers", CreateConsumer).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapDelete("/streams/{streamName}/consumers/{consumerName}", DeleteConsumer).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);

        return app;
    }

    private static async Task<IResult> GetStreams(
        Guid envId,
        [AsParameters] GetStreamsQueryParams queryParams,
        IUseCase<GetStreamsQuery, PaginatedResult<StreamListItem>> useCase,
        CancellationToken cancellationToken)
    {
        var query = new GetStreamsQuery
        {
            EnvironmentId = envId,
            Page = queryParams.Page ?? 1,
            PageSize = queryParams.PageSize ?? 25,
            SortBy = queryParams.SortBy,
            SortDescending = string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase),
            Search = queryParams.Search
        };

        var presenter = new Presenter<PaginatedResult<StreamListItem>>();
        await useCase.ExecuteAsync(query, presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetStreamDetail(
        Guid envId,
        string streamName,
        IUseCase<GetStreamDetailQuery, StreamDetailResult> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<StreamDetailResult>();
        await useCase.ExecuteAsync(new GetStreamDetailQuery(envId, streamName), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetConsumers(
        Guid envId,
        string streamName,
        [AsParameters] GetConsumersQueryParams queryParams,
        IUseCase<GetConsumersQuery, PaginatedResult<ConsumerInfo>> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<PaginatedResult<ConsumerInfo>>();
        await useCase.ExecuteAsync(new GetConsumersQuery(envId, streamName)
        {
            Page = queryParams.Page ?? 1,
            PageSize = queryParams.PageSize ?? 25,
            SortBy = queryParams.SortBy,
            SortDescending = string.Equals(queryParams.SortOrder, "desc", StringComparison.OrdinalIgnoreCase),
            Search = queryParams.Search
        }, presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetConsumerDetail(
        Guid envId,
        string streamName,
        string consumerName,
        IUseCase<GetConsumerDetailQuery, ConsumerInfo> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ConsumerInfo>();
        await useCase.ExecuteAsync(new GetConsumerDetailQuery(envId, streamName, consumerName), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetStreamMessages(
        Guid envId,
        string streamName,
        [FromQuery] long? startSequence,
        [FromQuery] int? count,
        IUseCase<GetStreamMessagesQuery, IReadOnlyList<StreamMessage>> useCase,
        CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<StreamMessage>>();
        await useCase.ExecuteAsync(new GetStreamMessagesQuery(envId, streamName, startSequence, count ?? 25), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> CreateStream(
        Guid envId,
        CreateStreamRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<CreateStreamCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var command = new CreateStreamCommand
        {
            EnvironmentId = envId,
            Name = request.Name,
            Description = request.Description,
            Subjects = request.Subjects,
            RetentionPolicy = request.RetentionPolicy ?? "Limits",
            StorageType = request.StorageType ?? "File",
            MaxMessages = request.MaxMessages ?? -1,
            MaxBytes = request.MaxBytes ?? -1,
            Replicas = request.Replicas ?? 1,
            DiscardPolicy = request.DiscardPolicy ?? "Old",
        };

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{envId}/jetstream/streams/{command.Name}");
    }

    private static async Task<IResult> UpdateStream(
        Guid envId,
        string streamName,
        UpdateStreamRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<UpdateStreamCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var command = new UpdateStreamCommand
        {
            EnvironmentId = envId,
            Name = streamName,
            Description = request.Description,
            Subjects = request.Subjects,
            MaxMessages = request.MaxMessages ?? -1,
            MaxBytes = request.MaxBytes ?? -1,
            Replicas = request.Replicas ?? 1,
        };

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> DeleteStream(
        Guid envId,
        string streamName,
        HttpContext httpContext,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteStreamCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required for destructive operations" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteStreamCommand { EnvironmentId = envId, Name = streamName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> PurgeStream(
        Guid envId,
        string streamName,
        [FromHeader(Name = "X-Confirm")] string? confirm,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<PurgeStreamCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required for destructive operations" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new PurgeStreamCommand { EnvironmentId = envId, Name = streamName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }

    private static async Task<IResult> CreateConsumer(
        Guid envId,
        string streamName,
        CreateConsumerRequest request,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<CreateConsumerCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var command = new CreateConsumerCommand
        {
            EnvironmentId = envId,
            StreamName = streamName,
            Name = request.Name,
            Description = request.Description,
            DeliverPolicy = request.DeliverPolicy ?? "All",
            AckPolicy = request.AckPolicy ?? "Explicit",
            FilterSubject = request.FilterSubject,
            MaxDeliver = request.MaxDeliver ?? -1,
        };

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(command, presenter, cancellationToken);
        return presenter.ToCreatedResult($"/api/environments/{envId}/jetstream/streams/{streamName}/consumers/{command.Name}");
    }

    private static async Task<IResult> DeleteConsumer(
        Guid envId,
        string streamName,
        string consumerName,
        HttpContext httpContext,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<DeleteConsumerCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var confirm = httpContext.Request.Headers["X-Confirm"].FirstOrDefault();
        if (!string.Equals(confirm, "true", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "X-Confirm: true header is required for destructive operations" });

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new DeleteConsumerCommand { EnvironmentId = envId, StreamName = streamName, Name = consumerName }, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }
}

public sealed record GetStreamsQueryParams
{
    [FromQuery] public int? Page { get; init; }
    [FromQuery] public int? PageSize { get; init; }
    [FromQuery] public string? SortBy { get; init; }
    [FromQuery] public string? SortOrder { get; init; }
    [FromQuery] public string? Search { get; init; }
}

public sealed record GetConsumersQueryParams
{
    [FromQuery] public int? Page { get; init; }
    [FromQuery] public int? PageSize { get; init; }
    [FromQuery] public string? SortBy { get; init; }
    [FromQuery] public string? SortOrder { get; init; }
    [FromQuery] public string? Search { get; init; }
}

public sealed record CreateStreamRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> Subjects,
    string? RetentionPolicy,
    string? StorageType,
    long? MaxMessages,
    long? MaxBytes,
    int? Replicas,
    string? DiscardPolicy);

public sealed record UpdateStreamRequest(
    string? Description,
    IReadOnlyList<string> Subjects,
    long? MaxMessages,
    long? MaxBytes,
    int? Replicas);

public sealed record CreateConsumerRequest(
    string Name,
    string? Description,
    string? DeliverPolicy,
    string? AckPolicy,
    string? FilterSubject,
    int? MaxDeliver);

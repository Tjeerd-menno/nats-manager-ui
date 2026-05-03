using System.Text.Json;
using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Commands;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.CoreNats.Queries;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class CoreNatsEndpoints
{
    public static IEndpointRouteBuilder MapCoreNatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/core-nats")
            .WithTags("Core NATS")
            .RequireAuthorization();

        group.MapGet("/status", GetStatus);
        group.MapGet("/subjects", GetSubjects);
        group.MapGet("/clients", GetClients);
        group.MapPost("/publish", PublishMessage).RequireAuthorization(AuthorizationPolicyNames.OperatorAccess);
        group.MapGet("/stream", StreamMessages);

        return app;
    }

    private static async Task<IResult> GetStatus(Guid envId, IUseCase<GetCoreStatusQuery, NatsServerInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<NatsServerInfo>();
        await useCase.ExecuteAsync(new GetCoreStatusQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetSubjects(Guid envId, IUseCase<GetSubjectsQuery, ListSubjectsResult> useCase, HttpResponse response, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<ListSubjectsResult>();
        await useCase.ExecuteAsync(new GetSubjectsQuery(envId), presenter, cancellationToken);
        if (!presenter.IsSuccess) return presenter.ToResult();

        var result = presenter.Value!;
        response.Headers["X-Subjects-Source"] = result.IsMonitoringAvailable ? "monitoring" : "unavailable";
        return Results.Ok(result.Subjects);
    }

    private static async Task<IResult> GetClients(Guid envId, IUseCase<GetClientsQuery, IReadOnlyList<NatsClientInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<NatsClientInfo>>();
        await useCase.ExecuteAsync(new GetClientsQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> PublishMessage(
        Guid envId,
        PublishMessageBody body,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        IUseCase<PublishMessageCommand, Unit> useCase,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = body.Subject,
            Payload = body.Payload,
            PayloadFormat = body.PayloadFormat,
            Headers = body.Headers ?? [],
            ReplyTo = body.ReplyTo,
        }, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Ok(new { published = true });
        return presenter.ToResult();
    }

    private static readonly System.Text.Json.JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    private static async Task<IResult> StreamMessages(
        Guid envId,
        string? subject,
        ICoreNatsAdapter adapter,
        ClaimsPrincipal user,
        IEnvironmentRepository environmentRepository,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var guardResult = await HighImpactActionGuard.RequireAllowedAsync(envId, user, environmentRepository, cancellationToken);
        if (guardResult is not null) return guardResult;

        if (string.IsNullOrWhiteSpace(subject))
        {
            return ApiProblemResults.ValidationProblem("subject", "Subject pattern must not be empty.");
        }

        if (subject.Contains(' '))
        {
            return ApiProblemResults.ValidationProblem("subject", "Subject pattern must not contain spaces.");
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            context.Response.Headers[HeaderNames.Connection] = "keep-alive";
            return Task.CompletedTask;
        });

        return Results.Stream(async stream =>
        {
            await foreach (var msg in adapter.SubscribeAsync(envId, subject, cancellationToken))
            {
                var json = JsonSerializer.Serialize(msg, CamelCaseOptions);
                var bytes = System.Text.Encoding.UTF8.GetBytes($"event: message\ndata: {json}\n\n");
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
        }, "text/event-stream");
    }
}

public sealed record PublishMessageBody(
    string Subject,
    string? Payload,
    PayloadFormat PayloadFormat = PayloadFormat.PlainText,
    Dictionary<string, string>? Headers = null,
    string? ReplyTo = null);

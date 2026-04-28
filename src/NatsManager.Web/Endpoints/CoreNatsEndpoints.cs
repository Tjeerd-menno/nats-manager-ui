using System.Text.Json;
using Microsoft.Net.Http.Headers;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Commands;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.CoreNats.Queries;
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

    private static async Task<IResult> PublishMessage(Guid envId, PublishMessageBody body, IUseCase<PublishMessageCommand, Unit> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<Unit>();
        await useCase.ExecuteAsync(new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = body.Subject,
            Payload = body.Payload,
            PayloadFormat = body.PayloadFormat,
            Headers = body.Headers ?? new Dictionary<string, string>(),
            ReplyTo = body.ReplyTo,
        }, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Ok(new { published = true });
        return presenter.ToResult();
    }

    private static readonly System.Text.Json.JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
    };

    private static async Task StreamMessages(
        Guid envId,
        string? subject,
        ICoreNatsAdapter adapter,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 400,
                title = "Bad Request",
                detail = "Subject pattern must not be empty."
            }, cancellationToken);
            return;
        }

        if (subject.Contains(' '))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 400,
                title = "Bad Request",
                detail = "Subject pattern must not contain spaces."
            }, cancellationToken);
            return;
        }

        context.Response.Headers[HeaderNames.ContentType] = "text/event-stream";
        context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.Headers[HeaderNames.Connection] = "keep-alive";

        await foreach (var msg in adapter.SubscribeAsync(envId, subject, cancellationToken))
        {
            var json = JsonSerializer.Serialize(msg, CamelCaseOptions);
            await context.Response.WriteAsync($"event: message\ndata: {json}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
}

public sealed record PublishMessageBody(
    string Subject,
    string? Payload,
    PayloadFormat PayloadFormat = PayloadFormat.PlainText,
    Dictionary<string, string>? Headers = null,
    string? ReplyTo = null);

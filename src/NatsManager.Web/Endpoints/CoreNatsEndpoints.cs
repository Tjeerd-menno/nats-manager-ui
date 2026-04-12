using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Commands;
using NatsManager.Application.Modules.CoreNats.Models;
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

        return app;
    }

    private static async Task<IResult> GetStatus(Guid envId, IUseCase<GetCoreStatusQuery, NatsServerInfo> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<NatsServerInfo>();
        await useCase.ExecuteAsync(new GetCoreStatusQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
    }

    private static async Task<IResult> GetSubjects(Guid envId, IUseCase<GetSubjectsQuery, IReadOnlyList<NatsSubjectInfo>> useCase, CancellationToken cancellationToken)
    {
        var presenter = new Presenter<IReadOnlyList<NatsSubjectInfo>>();
        await useCase.ExecuteAsync(new GetSubjectsQuery(envId), presenter, cancellationToken);
        return presenter.ToResult();
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
        }, presenter, cancellationToken);
        if (presenter.IsSuccess) return Results.Ok(new { published = true });
        return presenter.ToResult();
    }
}

public sealed record PublishMessageBody(string Subject, string? Payload);

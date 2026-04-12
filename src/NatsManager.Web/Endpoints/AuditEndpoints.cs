using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Queries;
using NatsManager.Domain.Modules.Common;
using NatsManager.Web.Presenters;
using NatsManager.Web.Security;

namespace NatsManager.Web.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/audit")
            .RequireAuthorization(AuthorizationPolicyNames.AuditRead);

        group.MapGet("/events", GetAuditEvents);
    }

    private static async Task<IResult> GetAuditEvents(
        IUseCase<GetAuditEventsQuery, AuditEventsResult> useCase,
        int page = 1,
        int pageSize = 50,
        Guid? actorId = null,
        ActionType? actionType = null,
        ResourceType? resourceType = null,
        Guid? environmentId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AuditSource? source = null,
        CancellationToken cancellationToken = default)
    {
        var presenter = new Presenter<AuditEventsResult>();
        await useCase.ExecuteAsync(new GetAuditEventsQuery
        {
            Page = page,
            PageSize = pageSize,
            ActorId = actorId,
            ActionType = actionType,
            ResourceType = resourceType,
            EnvironmentId = environmentId,
            FromDate = fromDate,
            ToDate = toDate,
            Source = source
        }, presenter, cancellationToken);

        return presenter.ToResult();
    }
}

using NatsManager.Application.Common;
using NatsManager.Application.Modules.Dashboard.Models;
using NatsManager.Application.Modules.Dashboard.Queries;

namespace NatsManager.Web.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments/{envId:guid}/monitoring")
            .WithTags("Dashboard")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard);

        return app;
    }

    private static async Task<IResult> GetDashboard(Guid envId, IUseCase<GetDashboardQuery, DashboardSummary> useCase, CancellationToken cancellationToken)
    {
        return await useCase.ExecuteToResultAsync(new GetDashboardQuery(envId), cancellationToken);
    }
}

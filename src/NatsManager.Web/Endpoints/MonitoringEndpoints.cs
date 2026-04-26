using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;

namespace NatsManager.Web.Endpoints;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments")
            .WithTags("Monitoring")
            .RequireAuthorization();

        group.MapGet("/{envId:guid}/monitoring/metrics/history", GetMonitoringHistory);

        return app;
    }

    private static async Task<IResult> GetMonitoringHistory(
        Guid envId,
        IEnvironmentRepository environmentRepository,
        IMonitoringMetricsStore metricsStore,
        CancellationToken ct)
    {
        var environment = await environmentRepository.GetByIdAsync(envId, ct);
        if (environment is null)
            return Results.NotFound(new { error = $"Environment '{envId}' not found." });

        if (environment.MonitoringUrl is null)
            return Results.BadRequest(new { error = "Monitoring is not configured for this environment." });

        var history = metricsStore.GetHistory(envId);
        var result = new MonitoringHistoryResult(envId, history);
        return Results.Ok(result);
    }
}

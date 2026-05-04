using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

namespace NatsManager.Web.Endpoints;

public static class MonitoringEndpoints
{
    public static IEndpointRouteBuilder MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/environments")
            .WithTags("Monitoring")
            .RequireAuthorization();

        group.MapGet("/{envId:guid}/monitoring/metrics/history", GetMonitoringHistory);
        group.MapGet("/{environmentId:guid}/monitoring/cluster/overview", GetClusterOverview);
        group.MapGet("/{environmentId:guid}/monitoring/cluster/topology", GetClusterTopology);

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
            return ApiProblemResults.NotFound($"Environment '{envId}' not found.");

        if (environment.MonitoringUrl is null)
            return ApiProblemResults.BadRequest("Monitoring is not configured for this environment.");

        var history = metricsStore.GetHistory(envId);
        var result = new MonitoringHistoryResult(envId, history);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetClusterOverview(
        Guid environmentId,
        IEnvironmentRepository environmentRepository,
        IClusterObservationStore observationStore,
        CancellationToken ct)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, ct);
        if (environment is null)
            return ApiProblemResults.NotFound($"Environment '{environmentId}' not found.");

        if (environment.MonitoringUrl is null)
            return ApiProblemResults.BadRequest("Monitoring is not configured for this environment.");

        var handler = new GetClusterOverviewQueryHandler(observationStore);
        var observation = handler.Handle(new GetClusterOverviewQuery(environmentId));
        if (observation is null)
            return Results.Problem(
                detail: "No cluster observation data is available. All monitoring endpoints may be unavailable.",
                statusCode: 503,
                title: "Cluster Monitoring Unavailable");

        return Results.Ok(observation);
    }

    private static async Task<IResult> GetClusterTopology(
        Guid environmentId,
        IEnvironmentRepository environmentRepository,
        IClusterObservationStore observationStore,
        string? types,
        string? status,
        bool includeStale = true,
        int maxNodes = 250,
        CancellationToken ct = default)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, ct);
        if (environment is null)
            return ApiProblemResults.NotFound($"Environment '{environmentId}' not found.");

        if (environment.MonitoringUrl is null)
            return ApiProblemResults.BadRequest("Monitoring is not configured for this environment.");

        if (maxNodes is < 1 or > 1000)
            return ApiProblemResults.ValidationProblem("maxNodes", "maxNodes must be between 1 and 1000.");

        IReadOnlyList<TopologyRelationshipType>? typeFilter = null;
        if (!string.IsNullOrWhiteSpace(types))
        {
            var parsed = new List<TopologyRelationshipType>();
            foreach (var t in types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Enum.TryParse<TopologyRelationshipType>(t, ignoreCase: true, out var rt))
                    parsed.Add(rt);
                else
                    return ApiProblemResults.ValidationProblem("types", $"Invalid topology type: {t}");
            }
            typeFilter = parsed;
        }

        RelationshipStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RelationshipStatus>(status, ignoreCase: true, out var parsedStatus))
                return ApiProblemResults.ValidationProblem("status", $"Invalid relationship status: {status}");

            statusFilter = parsedStatus;
        }

        var handler = new GetClusterTopologyQueryHandler(observationStore);
        var result = handler.Handle(new GetClusterTopologyQuery(environmentId, typeFilter, statusFilter, includeStale, maxNodes));

        if (result is null)
            return Results.Problem(
                detail: "No topology data is available. Monitoring endpoints may be unavailable.",
                statusCode: 503,
                title: "Cluster Topology Unavailable");

        return Results.Ok(result);
    }
}


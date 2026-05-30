using NatsManager.Application.Common;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

/// <summary>Query to get the latest cluster overview for an environment.</summary>
public sealed record GetClusterOverviewQuery(Guid EnvironmentId);

/// <summary>Handler for GetClusterOverviewQuery.</summary>
public sealed class GetClusterOverviewQueryHandler(
    IClusterObservationStore store) : IUseCase<GetClusterOverviewQuery, ClusterObservation>
{
    public Task ExecuteAsync(
        GetClusterOverviewQuery request,
        IOutputPort<ClusterObservation> outputPort,
        CancellationToken cancellationToken = default)
    {
        var observation = store.GetLatest(request.EnvironmentId);
        if (observation is null)
            outputPort.Unavailable("No cluster observation data is available. All monitoring endpoints may be unavailable.");
        else
            outputPort.Success(observation);

        return Task.CompletedTask;
    }
}

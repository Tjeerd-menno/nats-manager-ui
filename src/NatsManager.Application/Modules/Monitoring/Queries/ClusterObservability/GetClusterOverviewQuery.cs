using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Application.Modules.Monitoring.Queries.ClusterObservability;

/// <summary>Query to get the latest cluster overview for an environment.</summary>
public sealed record GetClusterOverviewQuery(Guid EnvironmentId);

/// <summary>Handler for GetClusterOverviewQuery.</summary>
public sealed class GetClusterOverviewQueryHandler(
    IClusterObservationStore store)
{
    public ClusterObservation? Handle(GetClusterOverviewQuery query) =>
        store.GetLatest(query.EnvironmentId);
}

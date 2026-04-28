using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

namespace NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

public interface IClusterObservationStore
{
    /// <summary>Gets the latest cluster observation for the given environment, or null if none exists.</summary>
    ClusterObservation? GetLatest(Guid environmentId);

    /// <summary>Stores a new observation, replacing the previous within the retention window.</summary>
    void StoreObservation(ClusterObservation observation);

    /// <summary>Returns all retained observations for the given environment (bounded by MaxRetainedObservations).</summary>
    IReadOnlyList<ClusterObservation> GetRetained(Guid environmentId);
}

namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

/// <summary>
/// Helper class for deriving cluster-level health, freshness, and counter rates
/// from a collection of server observations.
/// </summary>
public static class ClusterHealthDerivation
{
    /// <summary>
    /// Aggregates individual server statuses into an overall ClusterStatus.
    /// </summary>
    public static ClusterStatus DeriveClusterStatus(IReadOnlyList<ServerObservation> servers)
    {
        if (servers.Count == 0)
            return ClusterStatus.Unknown;

        var hasHealthy = false;
        var hasDegraded = false;
        var allUnavailable = true;

        foreach (var s in servers)
        {
            switch (s.Status)
            {
                case ServerStatus.Healthy:
                    hasHealthy = true;
                    allUnavailable = false;
                    break;
                case ServerStatus.Warning or ServerStatus.Stale:
                    hasDegraded = true;
                    allUnavailable = false;
                    break;
                case ServerStatus.Unavailable:
                    hasDegraded = true;
                    break;
                case ServerStatus.Unknown:
                    allUnavailable = false;
                    break;
            }
        }

        if (allUnavailable)
            return ClusterStatus.Unavailable;
        if (hasDegraded)
            return ClusterStatus.Degraded;
        if (hasHealthy)
            return ClusterStatus.Healthy;
        return ClusterStatus.Unknown;
    }

    /// <summary>
    /// Derives the overall observation freshness from server observations.
    /// </summary>
    public static ObservationFreshness DeriveFreshness(IReadOnlyList<ServerObservation> servers)
    {
        if (servers.Count == 0)
            return ObservationFreshness.Unavailable;

        var live = 0;
        var stale = 0;
        var unavailable = 0;

        foreach (var s in servers)
        {
            switch (s.Freshness)
            {
                case ObservationFreshness.Live: live++; break;
                case ObservationFreshness.Stale: stale++; break;
                case ObservationFreshness.Partial: stale++; break;
                case ObservationFreshness.Unavailable: unavailable++; break;
            }
        }

        if (unavailable == servers.Count)
            return ObservationFreshness.Unavailable;
        if (live == servers.Count)
            return ObservationFreshness.Live;
        if (stale > 0 || unavailable > 0)
            return ObservationFreshness.Partial;
        return ObservationFreshness.Live;
    }

    /// <summary>
    /// Derives a counter rate from two consecutive snapshots.
    /// Returns null if either baseline is unavailable or values would be negative.
    /// </summary>
    public static double? DeriveRate(long? current, long? previous, double elapsedSeconds)
    {
        if (current is null || previous is null || elapsedSeconds <= 0)
            return null;

        var delta = current.Value - previous.Value;
        if (delta < 0)
            return null; // Counter reset or unavailable baseline

        return delta / elapsedSeconds;
    }

    /// <summary>
    /// Determines ObservationFreshness based on time since last observation.
    /// </summary>
    public static ObservationFreshness DeriveServerFreshness(
        DateTimeOffset lastObservedAt,
        DateTimeOffset now,
        int staleThresholdSeconds)
    {
        var age = (now - lastObservedAt).TotalSeconds;
        return age > staleThresholdSeconds
            ? ObservationFreshness.Stale
            : ObservationFreshness.Live;
    }
}

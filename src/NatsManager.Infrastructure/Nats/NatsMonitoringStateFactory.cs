using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

namespace NatsManager.Infrastructure.Nats;

internal static class NatsMonitoringStateFactory
{
    private static readonly ServerMetrics EmptyServerMetrics = new(
        Version: string.Empty,
        Connections: 0,
        TotalConnections: 0,
        MaxConnections: 0,
        InMsgsTotal: 0,
        OutMsgsTotal: 0,
        InBytesTotal: 0,
        OutBytesTotal: 0,
        InMsgsPerSec: 0,
        OutMsgsPerSec: 0,
        InBytesPerSec: 0,
        OutBytesPerSec: 0,
        UptimeSeconds: 0,
        MemoryBytes: 0);

    public static MonitoringSnapshot CreateSnapshot(Guid environmentId, MonitoringStatus status, MonitoringStatus healthStatus) =>
        new(environmentId, DateTimeOffset.UtcNow, EmptyServerMetrics, null, status, healthStatus);

    public static ClusterObservation CreateUnavailableClusterObservation(Guid environmentId) =>
        new(
            EnvironmentId: environmentId,
            ObservedAt: DateTimeOffset.UtcNow,
            Status: ClusterStatus.Unavailable,
            Freshness: ObservationFreshness.Unavailable,
            ServerCount: 0,
            DegradedServerCount: 0,
            JetStreamAvailable: null,
            ConnectionCount: null,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            Warnings: [],
            Servers: [],
            Topology: []);
}

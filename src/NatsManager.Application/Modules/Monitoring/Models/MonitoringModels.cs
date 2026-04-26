namespace NatsManager.Application.Modules.Monitoring.Models;

public enum MonitoringStatus { Ok, Degraded, Unavailable }

public sealed record ServerMetrics(
    string Version,
    int Connections,
    long TotalConnections,
    int MaxConnections,
    long InMsgsTotal,
    long OutMsgsTotal,
    long InBytesTotal,
    long OutBytesTotal,
    double InMsgsPerSec,
    double OutMsgsPerSec,
    double InBytesPerSec,
    double OutBytesPerSec,
    long UptimeSeconds,
    long MemoryBytes);

public sealed record JetStreamMetrics(
    int StreamCount,
    int ConsumerCount,
    long TotalMessages,
    long TotalBytes);

public sealed record MonitoringSnapshot(
    Guid EnvironmentId,
    DateTimeOffset Timestamp,
    ServerMetrics Server,
    JetStreamMetrics? JetStream,
    MonitoringStatus Status,
    MonitoringStatus HealthStatus);

public sealed record MonitoringHistoryResult(
    Guid EnvironmentId,
    IReadOnlyList<MonitoringSnapshot> Snapshots);

namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

public sealed record ServerObservation(
    Guid EnvironmentId,
    string ServerId,
    string? ServerName,
    string? ClusterName,
    string? Version,
    long? UptimeSeconds,
    ServerStatus Status,
    ObservationFreshness Freshness,
    int? Connections,
    int? MaxConnections,
    int? SlowConsumers,
    long? MemoryBytes,
    long? StorageBytes,
    double? InMsgsPerSecond,
    double? OutMsgsPerSecond,
    double? InBytesPerSecond,
    double? OutBytesPerSecond,
    DateTimeOffset LastObservedAt,
    IReadOnlyList<MetricState> MetricStates);

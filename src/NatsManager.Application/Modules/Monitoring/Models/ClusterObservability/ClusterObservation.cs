namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

public sealed record ClusterObservation(
    Guid EnvironmentId,
    DateTimeOffset ObservedAt,
    ClusterStatus Status,
    ObservationFreshness Freshness,
    int ServerCount,
    int DegradedServerCount,
    bool? JetStreamAvailable,
    int? ConnectionCount,
    double? InMsgsPerSecond,
    double? OutMsgsPerSecond,
    IReadOnlyList<ClusterWarning> Warnings,
    IReadOnlyList<ServerObservation> Servers,
    IReadOnlyList<TopologyRelationship> Topology);

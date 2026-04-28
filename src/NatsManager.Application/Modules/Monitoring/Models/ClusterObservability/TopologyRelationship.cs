namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

public sealed record TopologyRelationship(
    Guid EnvironmentId,
    string RelationshipId,
    string SourceNodeId,
    string TargetNodeId,
    TopologyRelationshipType Type,
    RelationshipDirection Direction,
    RelationshipStatus Status,
    ObservationFreshness Freshness,
    DateTimeOffset ObservedAt,
    MonitoringEndpoint SourceEndpoint,
    string SafeLabel);

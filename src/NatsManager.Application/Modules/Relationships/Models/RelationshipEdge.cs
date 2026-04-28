namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>
/// A relationship between two resource nodes.
/// EdgeId is deterministic from source/target/type/evidence source.
/// Inferred edges must include evidence and confidence.
/// Direction is Unknown when not safely determined.
/// </summary>
public sealed record RelationshipEdge(
    string EdgeId,
    Guid EnvironmentId,
    string SourceNodeId,
    string TargetNodeId,
    RelationshipType RelationshipType,
    RelationshipDirection Direction,
    ObservationKind ObservationKind,
    RelationshipConfidence Confidence,
    RelationshipFreshness Freshness,
    ResourceHealthStatus Status,
    IReadOnlyList<RelationshipEvidence> Evidence)
{
    public static string BuildEdgeId(string sourceNodeId, string targetNodeId, RelationshipType type, RelationshipSourceModule sourceModule) =>
        $"{sourceNodeId}__{type}__{targetNodeId}__{sourceModule}";
}

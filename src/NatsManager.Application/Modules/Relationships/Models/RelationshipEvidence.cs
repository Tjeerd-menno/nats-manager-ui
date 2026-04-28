namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>Safe metadata explaining a relationship edge.</summary>
public sealed record RelationshipEvidence(
    RelationshipSourceModule SourceModule,
    string EvidenceType,
    DateTimeOffset? ObservedAt,
    RelationshipFreshness Freshness,
    string Summary,
    IReadOnlyDictionary<string, string> SafeFields);

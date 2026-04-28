namespace NatsManager.Application.Modules.Relationships.Models;

public sealed record OmittedCounts(
    int FilteredNodes,
    int FilteredEdges,
    int CollapsedNodes,
    int CollapsedEdges,
    int UnsafeRelationships);

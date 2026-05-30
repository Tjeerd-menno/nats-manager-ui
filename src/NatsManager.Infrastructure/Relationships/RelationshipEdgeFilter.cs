using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Pure edge-eligibility predicate applied while expanding the BFS frontier: honors the inferred,
/// staleness, relationship-type, and minimum-confidence filters from <see cref="MapFilter"/>.
/// </summary>
internal static class RelationshipEdgeFilter
{
    public static bool PassesFilters(RelationshipEdge edge, MapFilter filters)
    {
        if (!filters.IncludeInferred && edge.ObservationKind == ObservationKind.Inferred)
            return false;

        if (!filters.IncludeStale && edge.Freshness == RelationshipFreshness.Stale)
            return false;

        if (filters.RelationshipTypes is { Count: > 0 } && !filters.RelationshipTypes.Contains(edge.RelationshipType))
            return false;

        if (filters.MinimumConfidence != RelationshipConfidence.Unknown)
        {
            var minLevel = (int)filters.MinimumConfidence;
            var edgeLevel = (int)edge.Confidence;
            // High=0, Medium=1, Low=2, Unknown=3 — we want confidence >= minimum
            if (edgeLevel > minLevel && edge.Confidence != RelationshipConfidence.Unknown)
                return false;
        }

        return true;
    }
}

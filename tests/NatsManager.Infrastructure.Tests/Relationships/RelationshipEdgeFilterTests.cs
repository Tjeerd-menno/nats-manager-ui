using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Infrastructure.Relationships;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class RelationshipEdgeFilterTests
{
    private static readonly Guid EnvironmentId = Guid.NewGuid();

    [Fact]
    public void PassesFilters_ExcludesInferredWhenIncludeInferredFalse()
    {
        var edge = CreateEdge(observationKind: ObservationKind.Inferred);
        var filters = MapFilter.Default with { IncludeInferred = false };

        RelationshipEdgeFilter.PassesFilters(edge, filters).ShouldBeFalse();
    }

    [Fact]
    public void PassesFilters_ExcludesStaleWhenIncludeStaleFalse()
    {
        var edge = CreateEdge(freshness: RelationshipFreshness.Stale);
        var filters = MapFilter.Default with { IncludeStale = false };

        RelationshipEdgeFilter.PassesFilters(edge, filters).ShouldBeFalse();
    }

    [Fact]
    public void PassesFilters_ExcludesUnmatchedRelationshipType()
    {
        var edge = CreateEdge(relationshipType: RelationshipType.UsesSubject);
        var filters = MapFilter.Default with { RelationshipTypes = [RelationshipType.BackedByStream] };

        RelationshipEdgeFilter.PassesFilters(edge, filters).ShouldBeFalse();
    }

    [Fact]
    public void PassesFilters_ExcludesEdgeBelowMinimumConfidence()
    {
        var edge = CreateEdge(confidence: RelationshipConfidence.Low);
        var filters = MapFilter.Default with { MinimumConfidence = RelationshipConfidence.High };

        RelationshipEdgeFilter.PassesFilters(edge, filters).ShouldBeFalse();
    }

    [Fact]
    public void PassesFilters_AllowsEdgeMeetingAllCriteria()
    {
        var edge = CreateEdge();

        RelationshipEdgeFilter.PassesFilters(edge, MapFilter.Default).ShouldBeTrue();
    }

    private static RelationshipEdge CreateEdge(
        RelationshipType relationshipType = RelationshipType.UsesSubject,
        ObservationKind observationKind = ObservationKind.Observed,
        RelationshipConfidence confidence = RelationshipConfidence.High,
        RelationshipFreshness freshness = RelationshipFreshness.Live) =>
        new(
            RelationshipEdge.BuildEdgeId("a", "b", relationshipType, RelationshipSourceModule.KeyValue),
            EnvironmentId,
            "a",
            "b",
            relationshipType,
            RelationshipDirection.Outbound,
            observationKind,
            confidence,
            freshness,
            ResourceHealthStatus.Healthy,
            []);
}

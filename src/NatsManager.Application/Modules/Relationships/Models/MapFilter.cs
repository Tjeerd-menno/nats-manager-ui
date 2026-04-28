using FluentValidation;

namespace NatsManager.Application.Modules.Relationships.Models;

/// <summary>
/// User-selected criteria controlling visible graph content.
/// Default values match data-model.md §6.
/// </summary>
public sealed record MapFilter(
    int Depth = 1,
    IReadOnlyList<ResourceType>? ResourceTypes = null,
    IReadOnlyList<RelationshipType>? RelationshipTypes = null,
    IReadOnlyList<ResourceHealthStatus>? HealthStates = null,
    RelationshipConfidence MinimumConfidence = RelationshipConfidence.Low,
    bool IncludeInferred = true,
    bool IncludeStale = true,
    int MaxNodes = 100,
    int MaxEdges = 500)
{
    public static MapFilter Default => new();
}

public sealed class MapFilterValidator : AbstractValidator<MapFilter>
{
    public MapFilterValidator()
    {
        RuleFor(f => f.Depth).InclusiveBetween(1, 3)
            .WithMessage("Depth must be between 1 and 3.");
        RuleFor(f => f.MaxNodes).InclusiveBetween(1, 500)
            .WithMessage("MaxNodes must be between 1 and 500.");
        RuleFor(f => f.MaxEdges).InclusiveBetween(1, 2000)
            .WithMessage("MaxEdges must be between 1 and 2000.");
    }
}

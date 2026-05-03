using NatsManager.Application.Modules.Relationships.Models;

namespace NatsManager.Application.Modules.Relationships.Queries;

public sealed record GetRelationshipNodeQuery(Guid EnvironmentId, string NodeId);

public sealed record RelationshipNodeDetails(
    string NodeId,
    ResourceType ResourceType,
    string ResourceId,
    string DisplayName,
    ResourceHealthStatus Status,
    RelationshipFreshness Freshness,
    string? DetailRoute,
    bool CanRecenter);

public sealed record RelationshipNodeResult(
    RelationshipNodeDetails? Node,
    string? NotFoundReason,
    string? ValidationError,
    RelationshipNodeRejectionReason? RejectionReason)
{
    public bool IsNotFound => Node is null && NotFoundReason is not null;
    public bool IsInvalid => ValidationError is not null;
}

public enum RelationshipNodeRejectionReason
{
    CrossEnvironment,
    InvalidNodeId,
    UnknownNodeType,
    NodeNotFound
}

using Microsoft.Extensions.Logging.Abstractions;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Application.Modules.Relationships.Queries;
using NatsManager.Infrastructure.Relationships;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Relationships;

public sealed class GetRelationshipNodeQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNodeIsCrossEnvironment_ShouldReturnNotFound()
    {
        var resolver = new FakeFocalResourceResolver();
        var handler = CreateHandler(resolver, []);
        var environmentId = Guid.NewGuid();
        var otherEnvironmentId = Guid.NewGuid();

        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(
            environmentId,
            ResourceNode.BuildNodeId(otherEnvironmentId, ResourceType.Stream, "orders")));

        result.IsNotFound.ShouldBeTrue();
        result.NotFoundReason.ShouldBe("Node was not found in this environment.");
        result.RejectionReason.ShouldBe(RelationshipNodeRejectionReason.CrossEnvironment);
        resolver.ResolveCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task HandleAsync_WhenNodeIdIsInvalid_ShouldReturnValidationError()
    {
        var environmentId = Guid.NewGuid();
        var handler = CreateHandler(new FakeFocalResourceResolver(), []);

        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(environmentId, $"{environmentId}:stream"));

        result.IsInvalid.ShouldBeTrue();
        result.ValidationError.ShouldBe("Invalid node id.");
        result.RejectionReason.ShouldBe(RelationshipNodeRejectionReason.InvalidNodeId);
    }

    [Fact]
    public async Task HandleAsync_WhenNodeTypeIsUnknown_ShouldReturnValidationError()
    {
        var environmentId = Guid.NewGuid();
        var handler = CreateHandler(new FakeFocalResourceResolver(), []);

        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(environmentId, $"{environmentId}:Bogus:orders"));

        result.IsInvalid.ShouldBeTrue();
        result.ValidationError.ShouldBe("Unknown resource type in node id: 'Bogus'.");
        result.RejectionReason.ShouldBe(RelationshipNodeRejectionReason.UnknownNodeType);
    }

    [Fact]
    public async Task HandleAsync_WhenResourceIsNotFound_ShouldReturnNodeNotFoundReason()
    {
        var environmentId = Guid.NewGuid();
        var handler = CreateHandler(new FakeFocalResourceResolver(), []);

        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(
            environmentId,
            ResourceNode.BuildNodeId(environmentId, ResourceType.Stream, "orders")));

        result.IsNotFound.ShouldBeTrue();
        result.NotFoundReason.ShouldBe($"Resource 'Stream:orders' not found in environment {environmentId}.");
        result.RejectionReason.ShouldBe(RelationshipNodeRejectionReason.NodeNotFound);
    }

    [Fact]
    public async Task HandleAsync_WhenNodeExists_ShouldReturnProjectedNodeStatus()
    {
        var environmentId = Guid.NewGuid();
        var nodeId = ResourceNode.BuildNodeId(environmentId, ResourceType.Stream, "orders");
        var focal = new FocalResource(environmentId, ResourceType.Stream, "orders", "orders", "/jetstream/streams/orders");
        var resolver = new FakeFocalResourceResolver { FocalResource = focal };
        var source = new FakeRelationshipSource([
                new ResourceNode(
                    nodeId,
                    environmentId,
                    ResourceType.Stream,
                    "orders",
                    "orders",
                    ResourceHealthStatus.Warning,
                    RelationshipFreshness.Live,
                    false,
                    "/jetstream/streams/orders",
                    new Dictionary<string, string>())
            ]);
        var handler = CreateHandler(resolver, [source]);

        var result = await handler.HandleAsync(new GetRelationshipNodeQuery(environmentId, nodeId));

        result.Node.ShouldNotBeNull();
        result.Node.NodeId.ShouldBe(nodeId);
        result.Node.ResourceType.ShouldBe(ResourceType.Stream);
        result.Node.ResourceId.ShouldBe("orders");
        result.Node.DisplayName.ShouldBe("orders");
        result.Node.Status.ShouldBe(ResourceHealthStatus.Warning);
        result.Node.Freshness.ShouldBe(RelationshipFreshness.Live);
        result.Node.DetailRoute.ShouldBe("/jetstream/streams/orders");
        result.Node.CanRecenter.ShouldBeTrue();
    }

    private static GetRelationshipNodeQueryHandler CreateHandler(
        IFocalResourceResolver resolver,
        IEnumerable<IRelationshipSource> sources)
    {
        var projectionService = new RelationshipProjectionService(
            sources,
            NullLogger<RelationshipProjectionService>.Instance);
        return new GetRelationshipNodeQueryHandler(resolver, projectionService);
    }

    private sealed class FakeFocalResourceResolver : IFocalResourceResolver
    {
        public FocalResource? FocalResource { get; init; }
        public int ResolveCallCount { get; private set; }

        public Task<FocalResource?> ResolveAsync(
            Guid environmentId,
            ResourceType resourceType,
            string resourceId,
            CancellationToken ct)
        {
            ResolveCallCount++;
            return Task.FromResult(FocalResource);
        }
    }

    private sealed class FakeRelationshipSource(IReadOnlyList<ResourceNode> nodes) : IRelationshipSource
    {
        public RelationshipSourceModule SourceModule => RelationshipSourceModule.JetStream;

        public Task<IReadOnlyList<RelationshipEdge>> GetEdgesForAsync(
            FocalResource focal,
            MapFilter filters,
            CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<RelationshipEdge>>([]);

        public Task<IReadOnlyList<ResourceNode>> ResolveNodesAsync(
            IEnumerable<string> nodeIds,
            Guid environmentId,
            CancellationToken ct) =>
            Task.FromResult(nodes);
    }
}

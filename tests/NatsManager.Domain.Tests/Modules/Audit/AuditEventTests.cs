using FluentAssertions;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Domain.Tests.Modules.Audit;

public sealed class AuditEventTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateAuditEvent()
    {
        var actorId = Guid.NewGuid();
        var envId = Guid.NewGuid();

        var auditEvent = AuditEvent.Create(
            actorId: actorId,
            actorName: "admin",
            actionType: ActionType.Create,
            resourceType: ResourceType.Stream,
            resourceId: "stream-1",
            resourceName: "My Stream",
            environmentId: envId,
            outcome: Outcome.Success,
            details: "Created stream",
            source: AuditSource.UserInitiated);

        auditEvent.Id.Should().NotBeEmpty();
        auditEvent.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        auditEvent.ActorId.Should().Be(actorId);
        auditEvent.ActorName.Should().Be("admin");
        auditEvent.ActionType.Should().Be(ActionType.Create);
        auditEvent.ResourceType.Should().Be(ResourceType.Stream);
        auditEvent.ResourceId.Should().Be("stream-1");
        auditEvent.ResourceName.Should().Be("My Stream");
        auditEvent.EnvironmentId.Should().Be(envId);
        auditEvent.Outcome.Should().Be(Outcome.Success);
        auditEvent.Details.Should().Be("Created stream");
        auditEvent.Source.Should().Be(AuditSource.UserInitiated);
    }

    [Fact]
    public void Create_WithNullActorId_ShouldAllowSystemGenerated()
    {
        var auditEvent = AuditEvent.Create(
            actorId: null,
            actorName: "system",
            actionType: ActionType.Update,
            resourceType: ResourceType.Environment,
            resourceId: "env-1",
            resourceName: "Env",
            environmentId: null,
            outcome: Outcome.Success,
            details: null,
            source: AuditSource.SystemGenerated);

        auditEvent.ActorId.Should().BeNull();
        auditEvent.EnvironmentId.Should().BeNull();
        auditEvent.Details.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidActorName_ShouldThrow(string? actorName)
    {
        var act = () => AuditEvent.Create(
            Guid.NewGuid(), actorName!, ActionType.Create,
            ResourceType.Stream, "id", "name", null, Outcome.Success, null, AuditSource.UserInitiated);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidResourceId_ShouldThrow(string? resourceId)
    {
        var act = () => AuditEvent.Create(
            Guid.NewGuid(), "admin", ActionType.Create,
            ResourceType.Stream, resourceId!, "name", null, Outcome.Success, null, AuditSource.UserInitiated);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidResourceName_ShouldThrow(string? resourceName)
    {
        var act = () => AuditEvent.Create(
            Guid.NewGuid(), "admin", ActionType.Create,
            ResourceType.Stream, "id", resourceName!, null, Outcome.Success, null, AuditSource.UserInitiated);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimStringFields()
    {
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(), "  admin  ", ActionType.Create,
            ResourceType.Stream, "  id  ", "  name  ", null, Outcome.Success, null, AuditSource.UserInitiated);

        auditEvent.ActorName.Should().Be("admin");
        auditEvent.ResourceId.Should().Be("id");
        auditEvent.ResourceName.Should().Be("name");
    }
}

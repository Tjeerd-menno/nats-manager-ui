using Shouldly;
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

        auditEvent.Id.ShouldNotBe(Guid.Empty);
        (auditEvent.Timestamp - DateTimeOffset.UtcNow).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(2));
        auditEvent.ActorId.ShouldBe(actorId);
        auditEvent.ActorName.ShouldBe("admin");
        auditEvent.ActionType.ShouldBe(ActionType.Create);
        auditEvent.ResourceType.ShouldBe(ResourceType.Stream);
        auditEvent.ResourceId.ShouldBe("stream-1");
        auditEvent.ResourceName.ShouldBe("My Stream");
        auditEvent.EnvironmentId.ShouldBe(envId);
        auditEvent.Outcome.ShouldBe(Outcome.Success);
        auditEvent.Details.ShouldBe("Created stream");
        auditEvent.Source.ShouldBe(AuditSource.UserInitiated);
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

        auditEvent.ActorId.ShouldBeNull();
        auditEvent.EnvironmentId.ShouldBeNull();
        auditEvent.Details.ShouldBeNull();
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

        Should.Throw<ArgumentException>(act);
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

        Should.Throw<ArgumentException>(act);
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

        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_ShouldTrimStringFields()
    {
        var auditEvent = AuditEvent.Create(
            Guid.NewGuid(), "  admin  ", ActionType.Create,
            ResourceType.Stream, "  id  ", "  name  ", null, Outcome.Success, null, AuditSource.UserInitiated);

        auditEvent.ActorName.ShouldBe("admin");
        auditEvent.ResourceId.ShouldBe("id");
        auditEvent.ResourceName.ShouldBe("name");
    }
}

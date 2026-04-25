using Shouldly;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Application.Modules.Audit.Queries;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Tests.Modules.Audit;

public sealed class GetAuditEventsQueryTests
{
    private readonly IAuditEventRepository _repository = Substitute.For<IAuditEventRepository>();
    private readonly GetAuditEventsQueryHandler _handler;

    public GetAuditEventsQueryTests()
    {
        _handler = new GetAuditEventsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult()
    {
        var ev = AuditEvent.Create(
            Guid.NewGuid(), "admin", ActionType.Create, ResourceType.Environment,
            "env-1", "Production", null, Outcome.Success, null, AuditSource.UserInitiated);

        _repository.GetPagedAsync(1, 50, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<AuditEvent> { ev } as IReadOnlyList<AuditEvent>, 1));

        var outputPort = new TestOutputPort<AuditEventsResult>();
        await _handler.ExecuteAsync(new GetAuditEventsQuery { Page = 1, PageSize = 50 }, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Items.Count().ShouldBe(1);
        outputPort.Value!.TotalCount.ShouldBe(1);
        outputPort.Value!.Page.ShouldBe(1);
        outputPort.Value!.PageSize.ShouldBe(50);
    }

    [Fact]
    public async Task Handle_ShouldMapToDto()
    {
        var actorId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var ev = AuditEvent.Create(
            actorId, "admin", ActionType.Delete, ResourceType.Stream,
            "stream-1", "Orders", envId, Outcome.Failure, "timeout", AuditSource.SystemGenerated);

        _repository.GetPagedAsync(1, 10, null, null, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns((new List<AuditEvent> { ev } as IReadOnlyList<AuditEvent>, 1));

        var outputPort = new TestOutputPort<AuditEventsResult>();
        await _handler.ExecuteAsync(new GetAuditEventsQuery { Page = 1, PageSize = 10 }, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        var dto = outputPort.Value!.Items[0];
        dto.ActorId.ShouldBe(actorId);
        dto.ActorName.ShouldBe("admin");
        dto.ActionType.ShouldBe(ActionType.Delete);
        dto.ResourceType.ShouldBe(ResourceType.Stream);
        dto.ResourceId.ShouldBe("stream-1");
        dto.EnvironmentId.ShouldBe(envId);
        dto.Outcome.ShouldBe(Outcome.Failure);
        dto.Details.ShouldBe("timeout");
        dto.Source.ShouldBe(AuditSource.SystemGenerated);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassToRepository()
    {
        var actorId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var to = DateTimeOffset.UtcNow;

        _repository.GetPagedAsync(2, 25, actorId, ActionType.Create, ResourceType.Stream, envId, from, to, AuditSource.UserInitiated, Arg.Any<CancellationToken>())
            .Returns((Array.Empty<AuditEvent>() as IReadOnlyList<AuditEvent>, 0));

        var query = new GetAuditEventsQuery
        {
            Page = 2,
            PageSize = 25,
            ActorId = actorId,
            ActionType = ActionType.Create,
            ResourceType = ResourceType.Stream,
            EnvironmentId = envId,
            FromDate = from,
            ToDate = to,
            Source = AuditSource.UserInitiated
        };

        var outputPort = new TestOutputPort<AuditEventsResult>();
        await _handler.ExecuteAsync(query, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Items.ShouldBeEmpty();
        outputPort.Value!.TotalCount.ShouldBe(0);
    }
}

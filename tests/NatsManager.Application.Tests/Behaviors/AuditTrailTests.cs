using Shouldly;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Tests.Behaviors;

public sealed class AuditTrailTests
{
    private readonly IAuditEventRepository _repository = Substitute.For<IAuditEventRepository>();
    private readonly IAuditContext _context = Substitute.For<IAuditContext>();
    private readonly AuditTrail _auditTrail;

    public AuditTrailTests()
    {
        _context.ActorId.Returns((Guid?)Guid.NewGuid());
        _context.ActorName.Returns("actor");
        _auditTrail = new AuditTrail(_repository, _context, NullLogger<AuditTrail>.Instance);
    }

    [Fact]
    public async Task RecordAsync_WithOutcomeOverload_ShouldPersist()
    {
        await _auditTrail.RecordAsync(
            ActionType.Login,
            ResourceType.User,
            "user-1",
            "user-1",
            environmentId: null,
            Outcome.Success,
            details: null,
            AuditSource.UserInitiated,
            CancellationToken.None);

        await _repository.Received(1).AddAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordAsync_WhenRepositoryThrows_ShouldSwallowException()
    {
        _repository.AddAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var act = async () => await _auditTrail.RecordAsync(
            ActionType.Login,
            ResourceType.User,
            "user-1",
            "user-1",
            environmentId: null,
            Outcome.Failure,
            details: null,
            AuditSource.UserInitiated,
            CancellationToken.None);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task RecordAsync_WhenRepositoryThrowsForAuditableCommand_ShouldSwallowException()
    {
        _repository.AddAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("DB down"));

        var command = Substitute.For<IAuditableCommand>();
        command.ActionType.Returns(ActionType.Update);
        command.ResourceType.Returns(ResourceType.Environment);
        command.ResourceId.Returns("env-1");
        command.ResourceName.Returns("env-1");
        command.EnvironmentId.Returns((Guid?)null);

        var act = async () => await _auditTrail.RecordAsync(command, CancellationToken.None);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task RecordAsync_WhenCancelled_ShouldPropagateOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repository.AddAsync(Arg.Any<AuditEvent>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        var act = async () => await _auditTrail.RecordAsync(
            ActionType.Login,
            ResourceType.User,
            "user-1",
            "user-1",
            environmentId: null,
            Outcome.Success,
            details: null,
            AuditSource.UserInitiated,
            cts.Token);

        await Should.ThrowAsync<OperationCanceledException>(act);
    }
}

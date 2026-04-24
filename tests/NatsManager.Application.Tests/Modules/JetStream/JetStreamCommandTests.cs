using Shouldly;
using NSubstitute;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Commands;
using NatsManager.Application.Modules.JetStream.Ports;

namespace NatsManager.Application.Tests.Modules.JetStream;

public sealed class CreateStreamCommandTests
{
    private readonly IJetStreamWriteAdapter _writeAdapter = Substitute.For<IJetStreamWriteAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly CreateStreamCommandHandler _handler;

    public CreateStreamCommandTests()
    {
        _handler = new CreateStreamCommandHandler(_writeAdapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToWriteAdapter()
    {
        var command = new CreateStreamCommand
        {
            EnvironmentId = Guid.NewGuid(),
            Name = "test-stream",
            Subjects = ["test.>"]
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _writeAdapter.Received(1).CreateStreamAsync(command, Arg.Any<CancellationToken>());
    }
}

public sealed class CreateStreamCommandValidatorTests
{
    private readonly CreateStreamCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CreateStreamCommand
        {
            EnvironmentId = Guid.NewGuid(),
            Name = "test-stream",
            Subjects = ["test.>"]
        };

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = new CreateStreamCommand
        {
            EnvironmentId = Guid.NewGuid(),
            Name = "",
            Subjects = ["test.>"]
        };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithEmptySubjects_ShouldFail()
    {
        var command = new CreateStreamCommand
        {
            EnvironmentId = Guid.NewGuid(),
            Name = "test",
            Subjects = []
        };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithZeroReplicas_ShouldFail()
    {
        var command = new CreateStreamCommand
        {
            EnvironmentId = Guid.NewGuid(),
            Name = "test",
            Subjects = ["test.>"],
            Replicas = 0
        };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }
}

public sealed class DeleteStreamCommandTests
{
    private readonly IJetStreamWriteAdapter _writeAdapter = Substitute.For<IJetStreamWriteAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteStreamCommandHandler _handler;

    public DeleteStreamCommandTests()
    {
        _handler = new DeleteStreamCommandHandler(_writeAdapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToWriteAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new DeleteStreamCommand { EnvironmentId = envId, Name = "test-stream" };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _writeAdapter.Received(1).DeleteStreamAsync(envId, "test-stream", Arg.Any<CancellationToken>());
    }
}

public sealed class PurgeStreamCommandTests
{
    private readonly IJetStreamWriteAdapter _writeAdapter = Substitute.For<IJetStreamWriteAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly PurgeStreamCommandHandler _handler;

    public PurgeStreamCommandTests()
    {
        _handler = new PurgeStreamCommandHandler(_writeAdapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToWriteAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new PurgeStreamCommand { EnvironmentId = envId, Name = "test-stream" };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _writeAdapter.Received(1).PurgeStreamAsync(envId, "test-stream", Arg.Any<CancellationToken>());
    }
}

public sealed class CreateConsumerCommandTests
{
    private readonly IJetStreamWriteAdapter _writeAdapter = Substitute.For<IJetStreamWriteAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly CreateConsumerCommandHandler _handler;

    public CreateConsumerCommandTests()
    {
        _handler = new CreateConsumerCommandHandler(_writeAdapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToWriteAdapter()
    {
        var command = new CreateConsumerCommand
        {
            EnvironmentId = Guid.NewGuid(),
            StreamName = "stream",
            Name = "consumer"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _writeAdapter.Received(1).CreateConsumerAsync(command, Arg.Any<CancellationToken>());
    }
}

public sealed class CreateConsumerCommandValidatorTests
{
    private readonly CreateConsumerCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CreateConsumerCommand
        {
            EnvironmentId = Guid.NewGuid(),
            StreamName = "stream",
            Name = "consumer"
        };

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyStreamName_ShouldFail()
    {
        var command = new CreateConsumerCommand
        {
            EnvironmentId = Guid.NewGuid(),
            StreamName = "",
            Name = "consumer"
        };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }
}

public sealed class DeleteConsumerCommandTests
{
    private readonly IJetStreamWriteAdapter _writeAdapter = Substitute.For<IJetStreamWriteAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteConsumerCommandHandler _handler;

    public DeleteConsumerCommandTests()
    {
        _handler = new DeleteConsumerCommandHandler(_writeAdapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToWriteAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new DeleteConsumerCommand
        {
            EnvironmentId = envId,
            StreamName = "stream",
            Name = "consumer"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _writeAdapter.Received(1).DeleteConsumerAsync(envId, "stream", "consumer", Arg.Any<CancellationToken>());
    }
}

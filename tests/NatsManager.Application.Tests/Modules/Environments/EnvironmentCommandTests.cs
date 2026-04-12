using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Commands;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Tests.Modules.Environments;

public sealed class RegisterEnvironmentCommandTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly ICredentialEncryptionService _encryption = Substitute.For<ICredentialEncryptionService>();
    private readonly INatsHealthChecker _healthChecker = Substitute.For<INatsHealthChecker>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly RegisterEnvironmentCommandHandler _handler;

    public RegisterEnvironmentCommandTests()
    {
        _healthChecker.CheckHealthAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>())
            .Returns(new TestConnectionResult(Reachable: true, LatencyMs: 5, ServerVersion: "2.10", JetStreamAvailable: true));
        _handler = new RegisterEnvironmentCommandHandler(_repository, _encryption, _healthChecker, _auditTrail);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateEnvironment()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "Test Env",
            ServerUrl = "nats://localhost:4222"
        };

        _repository.ExistsWithNameAsync(command.Name, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        var outputPort = new TestOutputPort<RegisterEnvironmentResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Id.Should().NotBeEmpty();
        outputPort.Value!.Name.Should().Be("Test Env");
        await _repository.Received(1).AddAsync(Arg.Any<Environment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldBeConflict()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "Existing",
            ServerUrl = "nats://localhost:4222"
        };

        _repository.ExistsWithNameAsync(command.Name, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(true);

        var outputPort = new TestOutputPort<RegisterEnvironmentResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithCredentials_ShouldEncryptCredential()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "Secure Env",
            ServerUrl = "nats://localhost:4222",
            CredentialType = CredentialType.Token,
            Credential = "my-secret-token"
        };

        _repository.ExistsWithNameAsync(command.Name, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);
        _encryption.Encrypt("my-secret-token").Returns("encrypted-token");

        var outputPort = new TestOutputPort<RegisterEnvironmentResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        _encryption.Received(1).Encrypt("my-secret-token");
        await _repository.Received(1).AddAsync(
            Arg.Is<Environment>(e => e.CredentialReference == "encrypted-token"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoCredentialType_ShouldNotEncrypt()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "No Cred",
            ServerUrl = "nats://localhost:4222",
            CredentialType = CredentialType.None
        };

        _repository.ExistsWithNameAsync(command.Name, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(false);

        var outputPort = new TestOutputPort<RegisterEnvironmentResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        _encryption.DidNotReceive().Encrypt(Arg.Any<string>());
    }
}

public sealed class RegisterEnvironmentCommandValidatorTests
{
    private readonly RegisterEnvironmentCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "Test",
            ServerUrl = "nats://localhost:4222"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "",
            ServerUrl = "nats://localhost:4222"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithCredentialTypeButNoCredential_ShouldFail()
    {
        var command = new RegisterEnvironmentCommand
        {
            Name = "Test",
            ServerUrl = "nats://localhost:4222",
            CredentialType = CredentialType.Token,
            Credential = ""
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}

public sealed class UpdateEnvironmentCommandTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly ICredentialEncryptionService _encryption = Substitute.For<ICredentialEncryptionService>();
    private readonly INatsConnectionFactory _connectionFactory = Substitute.For<INatsConnectionFactory>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly UpdateEnvironmentCommandHandler _handler;

    public UpdateEnvironmentCommandTests()
    {
        _handler = new UpdateEnvironmentCommandHandler(_repository, _encryption, _connectionFactory, _auditTrail);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateEnvironment()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Old", "nats://old:4222");
        var command = new UpdateEnvironmentCommand
        {
            Id = envId,
            Name = "New",
            ServerUrl = "nats://new:4222",
            IsEnabled = true
        };

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.ExistsWithNameAsync("New", envId, Arg.Any<CancellationToken>()).Returns(false);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _connectionFactory.Received(1).RemoveConnectionAsync(envId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldBeNotFound()
    {
        var command = new UpdateEnvironmentCommand
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            ServerUrl = "nats://localhost:4222"
        };

        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldBeConflict()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Old", "nats://old:4222");
        var command = new UpdateEnvironmentCommand
        {
            Id = envId,
            Name = "Duplicate",
            ServerUrl = "nats://localhost:4222"
        };

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.ExistsWithNameAsync("Duplicate", envId, Arg.Any<CancellationToken>()).Returns(true);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsConflict.Should().BeTrue();
    }
}

public sealed class DeleteEnvironmentCommandTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly INatsConnectionFactory _connectionFactory = Substitute.For<INatsConnectionFactory>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteEnvironmentCommandHandler _handler;

    public DeleteEnvironmentCommandTests()
    {
        _handler = new DeleteEnvironmentCommandHandler(_repository, _connectionFactory, _auditTrail);
    }

    [Fact]
    public async Task Handle_WithExistingEnvironment_ShouldDeleteAndRemoveConnection()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Test", "nats://localhost:4222");
        var command = new DeleteEnvironmentCommand { Id = envId };

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _connectionFactory.Received(1).RemoveConnectionAsync(envId, Arg.Any<CancellationToken>());
        await _repository.Received(1).DeleteAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldBeNotFound()
    {
        var command = new DeleteEnvironmentCommand { Id = Guid.NewGuid() };

        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class TestConnectionCommandTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly INatsHealthChecker _healthChecker = Substitute.For<INatsHealthChecker>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly TestConnectionCommandHandler _handler;

    public TestConnectionCommandTests()
    {
        _handler = new TestConnectionCommandHandler(_repository, _healthChecker, _auditTrail);
    }

    [Fact]
    public async Task Handle_WhenReachable_ShouldUpdateStatusToAvailable()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Test", "nats://localhost:4222");
        var command = new TestConnectionCommand { Id = envId };
        var healthResult = new TestConnectionResult(true, 5, "2.10.0", true);

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);
        _healthChecker.CheckHealthAsync(existing, Arg.Any<CancellationToken>()).Returns(healthResult);

        var outputPort = new TestOutputPort<TestConnectionResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Reachable.Should().BeTrue();
        outputPort.Value!.LatencyMs.Should().Be(5);
        outputPort.Value!.ServerVersion.Should().Be("2.10.0");
        outputPort.Value!.JetStreamAvailable.Should().BeTrue();
        existing.ConnectionStatus.Should().Be(ConnectionStatus.Available);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUnreachable_ShouldUpdateStatusToUnavailable()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Test", "nats://localhost:4222");
        var command = new TestConnectionCommand { Id = envId };
        var healthResult = new TestConnectionResult(false, null, null, false);

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);
        _healthChecker.CheckHealthAsync(existing, Arg.Any<CancellationToken>()).Returns(healthResult);

        var outputPort = new TestOutputPort<TestConnectionResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Reachable.Should().BeFalse();
        existing.ConnectionStatus.Should().Be(ConnectionStatus.Unavailable);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldBeNotFound()
    {
        var command = new TestConnectionCommand { Id = Guid.NewGuid() };
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var outputPort = new TestOutputPort<TestConnectionResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class EnableDisableEnvironmentCommandTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly INatsConnectionFactory _connectionFactory = Substitute.For<INatsConnectionFactory>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly EnableDisableEnvironmentCommandHandler _handler;

    public EnableDisableEnvironmentCommandTests()
    {
        _handler = new EnableDisableEnvironmentCommandHandler(_repository, _connectionFactory, _auditTrail);
    }

    [Fact]
    public async Task Handle_Enable_ShouldEnableEnvironment()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Test", "nats://localhost:4222");
        existing.Disable();
        var command = new EnableDisableEnvironmentCommand { Id = envId, Enable = true };

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        existing.IsEnabled.Should().BeTrue();
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Disable_ShouldDisableAndRemoveConnection()
    {
        var envId = Guid.NewGuid();
        var existing = Environment.Create("Test", "nats://localhost:4222");
        var command = new EnableDisableEnvironmentCommand { Id = envId, Enable = false };

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(existing);

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        existing.IsEnabled.Should().BeFalse();
        await _connectionFactory.Received(1).RemoveConnectionAsync(envId, Arg.Any<CancellationToken>());
    }
}

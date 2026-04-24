using Shouldly;
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

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Id.ShouldNotBe(Guid.Empty);
        outputPort.Value!.Name.ShouldBe("Test Env");
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

        outputPort.IsConflict.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
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

        result.IsValid.ShouldBeTrue();
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

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
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

        result.IsValid.ShouldBeFalse();
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

        outputPort.IsSuccess.ShouldBeTrue();
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

        outputPort.IsNotFound.ShouldBeTrue();
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

        outputPort.IsConflict.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
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

        outputPort.IsNotFound.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Reachable.ShouldBeTrue();
        outputPort.Value!.LatencyMs.ShouldBe(5);
        outputPort.Value!.ServerVersion.ShouldBe("2.10.0");
        outputPort.Value!.JetStreamAvailable.ShouldBeTrue();
        existing.ConnectionStatus.ShouldBe(ConnectionStatus.Available);
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

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Reachable.ShouldBeFalse();
        existing.ConnectionStatus.ShouldBe(ConnectionStatus.Unavailable);
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldBeNotFound()
    {
        var command = new TestConnectionCommand { Id = Guid.NewGuid() };
        _repository.GetByIdAsync(command.Id, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var outputPort = new TestOutputPort<TestConnectionResult>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsNotFound.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
        existing.IsEnabled.ShouldBeTrue();
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

        outputPort.IsSuccess.ShouldBeTrue();
        existing.IsEnabled.ShouldBeFalse();
        await _connectionFactory.Received(1).RemoveConnectionAsync(envId, Arg.Any<CancellationToken>());
    }
}

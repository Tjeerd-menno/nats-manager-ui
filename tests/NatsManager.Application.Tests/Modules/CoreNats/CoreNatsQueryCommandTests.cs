using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Modules.CoreNats.Commands;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.CoreNats.Queries;

namespace NatsManager.Application.Tests.Modules.CoreNats;

public sealed class GetCoreStatusQueryTests
{
    private readonly ICoreNatsAdapter _adapter = Substitute.For<ICoreNatsAdapter>();
    private readonly GetCoreStatusQueryHandler _handler;

    public GetCoreStatusQueryTests()
    {
        _handler = new GetCoreStatusQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnServerInfo()
    {
        var envId = Guid.NewGuid();
        var info = new NatsServerInfo("id1", "server1", "2.10.0", "localhost", 4222, 1048576, 5, 100, 50, 1024, 512, TimeSpan.FromHours(1), true);
        _adapter.GetServerInfoAsync(envId, Arg.Any<CancellationToken>()).Returns(info);

        var outputPort = new TestOutputPort<NatsServerInfo>();
        await _handler.ExecuteAsync(new GetCoreStatusQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.ServerName.Should().Be("server1");
    }

    [Fact]
    public async Task Handle_WhenUnavailable_ShouldReturnNotFound()
    {
        var envId = Guid.NewGuid();
        _adapter.GetServerInfoAsync(envId, Arg.Any<CancellationToken>()).Returns((NatsServerInfo?)null);

        var outputPort = new TestOutputPort<NatsServerInfo>();
        await _handler.ExecuteAsync(new GetCoreStatusQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class GetSubjectsQueryTests
{
    private readonly ICoreNatsAdapter _adapter = Substitute.For<ICoreNatsAdapter>();
    private readonly GetSubjectsQueryHandler _handler;

    public GetSubjectsQueryTests()
    {
        _handler = new GetSubjectsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnSubjects()
    {
        var envId = Guid.NewGuid();
        var subjects = new List<NatsSubjectInfo> { new("orders.>", 42) };
        _adapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>()).Returns(subjects);

        var outputPort = new TestOutputPort<IReadOnlyList<NatsSubjectInfo>>();
        await _handler.ExecuteAsync(new GetSubjectsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Subject.Should().Be("orders.>");
    }
}

public sealed class GetClientsQueryTests
{
    private readonly ICoreNatsAdapter _adapter = Substitute.For<ICoreNatsAdapter>();
    private readonly GetClientsQueryHandler _handler;

    public GetClientsQueryTests()
    {
        _handler = new GetClientsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnClients()
    {
        var envId = Guid.NewGuid();
        var clients = new List<NatsClientInfo> { new(1, "client-app", null, "127.0.0.1", 4222, 10, 5, 1024, 512, TimeSpan.FromMinutes(5)) };
        _adapter.ListClientsAsync(envId, Arg.Any<CancellationToken>()).Returns(clients);

        var outputPort = new TestOutputPort<IReadOnlyList<NatsClientInfo>>();
        await _handler.ExecuteAsync(new GetClientsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Name.Should().Be("client-app");
    }
}

public sealed class PublishMessageCommandTests
{
    private readonly ICoreNatsAdapter _adapter = Substitute.For<ICoreNatsAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly PublishMessageCommandHandler _handler;

    public PublishMessageCommandTests()
    {
        _handler = new PublishMessageCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_WithPayload_ShouldEncodeToUtf8AndPublish()
    {
        var envId = Guid.NewGuid();
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "orders.new",
            Payload = "hello"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "orders.new",
            Arg.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == "hello"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullPayload_ShouldPublishEmptyBytes()
    {
        var envId = Guid.NewGuid();
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "events.ping",
            Payload = null
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "events.ping",
            Arg.Is<byte[]>(b => b.Length == 0),
            Arg.Any<CancellationToken>());
    }
}

public sealed class PublishMessageCommandValidatorTests
{
    private readonly PublishMessageCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_WhenSubjectEmpty()
    {
        var command = new PublishMessageCommand { Subject = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new PublishMessageCommand { Subject = "test.subject" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

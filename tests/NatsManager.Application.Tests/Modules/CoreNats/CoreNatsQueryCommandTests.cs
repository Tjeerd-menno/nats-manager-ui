using Shouldly;
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

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.ServerName.ShouldBe("server1");
    }

    [Fact]
    public async Task Handle_WhenUnavailable_ShouldReturnNotFound()
    {
        var envId = Guid.NewGuid();
        _adapter.GetServerInfoAsync(envId, Arg.Any<CancellationToken>()).Returns((NatsServerInfo?)null);

        var outputPort = new TestOutputPort<NatsServerInfo>();
        await _handler.ExecuteAsync(new GetCoreStatusQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsNotFound.ShouldBeTrue();
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
        _adapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new ListSubjectsResult(subjects, IsMonitoringAvailable: true));

        var outputPort = new TestOutputPort<ListSubjectsResult>();
        await _handler.ExecuteAsync(new GetSubjectsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Subjects.Count.ShouldBe(1);
        outputPort.Value.Subjects[0].Subject.ShouldBe("orders.>");
        outputPort.Value.IsMonitoringAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenMonitoringUnavailable_ShouldReturnEmptyWithFlag()
    {
        var envId = Guid.NewGuid();
        _adapter.ListSubjectsAsync(envId, Arg.Any<CancellationToken>())
            .Returns(new ListSubjectsResult([], IsMonitoringAvailable: false));

        var outputPort = new TestOutputPort<ListSubjectsResult>();
        await _handler.ExecuteAsync(new GetSubjectsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Subjects.Count.ShouldBe(0);
        outputPort.Value.IsMonitoringAvailable.ShouldBeFalse();
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

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value.Count().ShouldBe(1);
        outputPort.Value![0].Name.ShouldBe("client-app");
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
            Payload = "hello",
            PayloadFormat = PayloadFormat.PlainText,
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "orders.new",
            Arg.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == "hello"),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<string?>(),
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

        outputPort.IsSuccess.ShouldBeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "events.ping",
            Arg.Is<byte[]>(b => b.Length == 0),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithHeaders_ShouldPassHeadersToAdapter()
    {
        var envId = Guid.NewGuid();
        var headers = new Dictionary<string, string> { ["X-Source"] = "test" };
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "orders.new",
            Payload = "hello",
            Headers = headers,
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "orders.new",
            Arg.Any<byte[]>(),
            Arg.Is<IReadOnlyDictionary<string, string>?>(h => h != null && h["X-Source"] == "test"),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithReplyTo_ShouldPassReplyToAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "orders.new",
            Payload = "hello",
            ReplyTo = "orders.reply",
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "orders.new",
            Arg.Any<byte[]>(),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            "orders.reply",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithJsonFormat_ValidJson_ShouldSucceed()
    {
        var envId = Guid.NewGuid();
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "orders.new",
            Payload = "{\"orderId\":\"abc\"}",
            PayloadFormat = PayloadFormat.Json,
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithHexBytesFormat_ShouldDecodeAndPublish()
    {
        var envId = Guid.NewGuid();
        var command = new PublishMessageCommand
        {
            EnvironmentId = envId,
            Subject = "orders.new",
            Payload = "48656C6C6F",  // "Hello" in hex
            PayloadFormat = PayloadFormat.HexBytes,
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        await _adapter.Received(1).PublishAsync(
            envId,
            "orders.new",
            Arg.Is<byte[]>(b => System.Text.Encoding.ASCII.GetString(b) == "Hello"),
            Arg.Any<IReadOnlyDictionary<string, string>?>(),
            Arg.Any<string?>(),
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
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new PublishMessageCommand { Subject = "test.subject" };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_Fail_WhenJsonFormat_InvalidJson()
    {
        var command = new PublishMessageCommand
        {
            Subject = "test.subject",
            Payload = "not-valid-json",
            PayloadFormat = PayloadFormat.Json,
        };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("JSON"));
    }

    [Fact]
    public void Should_Fail_WhenHexBytesFormat_InvalidHex()
    {
        var command = new PublishMessageCommand
        {
            Subject = "test.subject",
            Payload = "ZZZZ",
            PayloadFormat = PayloadFormat.HexBytes,
        };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("hex"));
    }

    [Fact]
    public void Should_Fail_WhenHeaderKeyIsEmpty()
    {
        var command = new PublishMessageCommand
        {
            Subject = "test.subject",
            Headers = new Dictionary<string, string> { [""] = "value" },
        };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Header key"));
    }

    [Fact]
    public void Should_Fail_WhenHeaderKeyIsWhitespace()
    {
        var command = new PublishMessageCommand
        {
            Subject = "test.subject",
            Headers = new Dictionary<string, string> { ["   "] = "value" },
        };
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Header key"));
    }
}

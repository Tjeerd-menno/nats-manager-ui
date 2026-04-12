using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.JetStream.Queries;

namespace NatsManager.Application.Tests.Modules.JetStream;

public sealed class GetStreamsQueryTests
{
    private readonly IJetStreamAdapter _adapter = Substitute.For<IJetStreamAdapter>();
    private readonly GetStreamsQueryHandler _handler;

    public GetStreamsQueryTests()
    {
        _handler = new GetStreamsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedStreams()
    {
        var envId = Guid.NewGuid();
        var streams = new List<StreamInfo>
        {
            new("stream-1", "Desc 1", ["sub.>"], "Limits", "File", 100, 1024, 2, DateTimeOffset.UtcNow,
                new StreamState(100, 1024, null, null, 1, 100)),
            new("stream-2", "Desc 2", ["events.>"], "WorkQueue", "Memory", 50, 512, 1, DateTimeOffset.UtcNow,
                new StreamState(50, 512, null, null, 1, 50))
        };

        _adapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(streams);

        var query = new GetStreamsQuery { EnvironmentId = envId, Page = 1, PageSize = 25 };
        var outputPort = new TestOutputPort<PaginatedResult<StreamListItem>>();
        await _handler.ExecuteAsync(query, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Items.Should().HaveCount(2);
        outputPort.Value!.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithSearch_ShouldFilter()
    {
        var envId = Guid.NewGuid();
        var streams = new List<StreamInfo>
        {
            new("orders", "Order stream", ["orders.>"], "Limits", "File", 100, 1024, 2, DateTimeOffset.UtcNow,
                new StreamState(100, 1024, null, null, 1, 100)),
            new("events", "Event stream", ["events.>"], "Limits", "File", 50, 512, 1, DateTimeOffset.UtcNow,
                new StreamState(50, 512, null, null, 1, 50))
        };

        _adapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(streams);

        var query = new GetStreamsQuery { EnvironmentId = envId, Search = "order" };
        var outputPort = new TestOutputPort<PaginatedResult<StreamListItem>>();
        await _handler.ExecuteAsync(query, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Items.Should().HaveCount(1);
        outputPort.Value!.Items[0].Name.Should().Be("orders");
    }

    [Fact]
    public async Task Handle_WithSortByMessages_ShouldSortCorrectly()
    {
        var envId = Guid.NewGuid();
        var streams = new List<StreamInfo>
        {
            new("low", "", [], "Limits", "File", 10, 100, 0, DateTimeOffset.UtcNow,
                new StreamState(10, 100, null, null, 1, 10)),
            new("high", "", [], "Limits", "File", 1000, 10000, 0, DateTimeOffset.UtcNow,
                new StreamState(1000, 10000, null, null, 1, 1000))
        };

        _adapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(streams);

        var query = new GetStreamsQuery { EnvironmentId = envId, SortBy = "messages", SortDescending = true };
        var outputPort = new TestOutputPort<PaginatedResult<StreamListItem>>();
        await _handler.ExecuteAsync(query, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Items[0].Name.Should().Be("high");
        outputPort.Value!.Items[1].Name.Should().Be("low");
    }
}

public sealed class GetStreamDetailQueryTests
{
    private readonly IJetStreamAdapter _adapter = Substitute.For<IJetStreamAdapter>();
    private readonly GetStreamDetailQueryHandler _handler;

    public GetStreamDetailQueryTests()
    {
        _handler = new GetStreamDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingStream_ShouldReturnDetail()
    {
        var envId = Guid.NewGuid();
        var info = new StreamInfo("test", "desc", ["sub.>"], "Limits", "File", 100, 1024, 1, DateTimeOffset.UtcNow,
            new StreamState(100, 1024, null, null, 1, 100));
        var config = new StreamConfig("test", "desc", ["sub.>"], "Limits", -1, -1, -1, "File", 1, "Old", -1, false, false, false);
        var consumers = new List<ConsumerInfo>();

        _adapter.GetStreamAsync(envId, "test", Arg.Any<CancellationToken>()).Returns(info);
        _adapter.GetStreamConfigAsync(envId, "test", Arg.Any<CancellationToken>()).Returns(config);
        _adapter.ListConsumersAsync(envId, "test", Arg.Any<CancellationToken>()).Returns(consumers);

        var outputPort = new TestOutputPort<StreamDetailResult>();
        await _handler.ExecuteAsync(new GetStreamDetailQuery(envId, "test"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Info.Should().Be(info);
        outputPort.Value!.Config.Should().Be(config);
        outputPort.Value!.Consumers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentStream_ShouldThrow()
    {
        var envId = Guid.NewGuid();
        _adapter.GetStreamAsync(envId, "missing", Arg.Any<CancellationToken>()).Returns((StreamInfo?)null);

        var outputPort = new TestOutputPort<StreamDetailResult>();
        await _handler.ExecuteAsync(new GetStreamDetailQuery(envId, "missing"), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class GetConsumerDetailQueryTests
{
    private readonly IJetStreamAdapter _adapter = Substitute.For<IJetStreamAdapter>();
    private readonly GetConsumerDetailQueryHandler _handler;

    public GetConsumerDetailQueryTests()
    {
        _handler = new GetConsumerDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingConsumer_ShouldReturn()
    {
        var envId = Guid.NewGuid();
        var consumer = new ConsumerInfo("stream", "consumer", null, "All", "Explicit", null, 0, 0, 0, true,
            DateTimeOffset.UtcNow, new ConsumerState(100, 100, 0, 0, 0));

        _adapter.GetConsumerAsync(envId, "stream", "consumer", Arg.Any<CancellationToken>()).Returns(consumer);

        var outputPort = new TestOutputPort<ConsumerInfo>();
        await _handler.ExecuteAsync(new GetConsumerDetailQuery(envId, "stream", "consumer"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().Be(consumer);
    }

    [Fact]
    public async Task Handle_WithNonExistentConsumer_ShouldThrow()
    {
        var envId = Guid.NewGuid();
        _adapter.GetConsumerAsync(envId, "stream", "missing", Arg.Any<CancellationToken>()).Returns((ConsumerInfo?)null);

        var outputPort = new TestOutputPort<ConsumerInfo>();
        await _handler.ExecuteAsync(new GetConsumerDetailQuery(envId, "stream", "missing"), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class GetConsumersQueryTests
{
    private readonly IJetStreamAdapter _adapter = Substitute.For<IJetStreamAdapter>();
    private readonly GetConsumersQueryHandler _handler;

    public GetConsumersQueryTests()
    {
        _handler = new GetConsumersQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnConsumers()
    {
        var envId = Guid.NewGuid();
        var consumers = new List<ConsumerInfo>
        {
            new("stream", "consumer-1", null, "All", "Explicit", null, 10, 2, 0, true,
                DateTimeOffset.UtcNow, new ConsumerState(100, 98, 10, 2, 0))
        };

        _adapter.ListConsumersAsync(envId, "stream", Arg.Any<CancellationToken>()).Returns(consumers);

        var outputPort = new TestOutputPort<IReadOnlyList<ConsumerInfo>>();
        await _handler.ExecuteAsync(new GetConsumersQuery(envId, "stream"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Name.Should().Be("consumer-1");
    }
}

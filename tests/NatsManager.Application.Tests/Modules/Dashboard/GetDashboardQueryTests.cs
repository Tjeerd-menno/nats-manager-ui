using Shouldly;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Dashboard.Models;
using NatsManager.Application.Modules.Dashboard.Queries;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Models;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Tests.Modules.Dashboard;

public sealed class GetDashboardQueryTests
{
    private readonly IEnvironmentRepository _envRepo = Substitute.For<IEnvironmentRepository>();
    private readonly IJetStreamAdapter _jsAdapter = Substitute.For<IJetStreamAdapter>();
    private readonly IKvStoreAdapter _kvAdapter = Substitute.For<IKvStoreAdapter>();
    private readonly GetDashboardQueryHandler _handler;

    public GetDashboardQueryTests()
    {
        _handler = new GetDashboardQueryHandler(_envRepo, _jsAdapter, _kvAdapter);
    }

    [Fact]
    public async Task Handle_ShouldAggregateEnvironmentHealth()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://localhost:4222");
        env.UpdateConnectionStatus(ConnectionStatus.Available);

        _envRepo.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _jsAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<StreamInfo>());
        _kvAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<KvBucketInfo>());

        var outputPort = new TestOutputPort<DashboardSummary>();
        await _handler.ExecuteAsync(new GetDashboardQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Environment.ConnectionStatus.ShouldBe("Available");
        outputPort.Value!.Environment.LastSuccessfulContact.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldAggregateJetStreamSummary()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://localhost:4222");

        var streams = new List<StreamInfo>
        {
            new("s1", "", [], "Limits", "File", 100, 1024, 1, DateTimeOffset.UtcNow,
                new StreamState(100, 1024, null, null, 1, 100)),
            new("s2", "", [], "Limits", "File", 200, 2048, 2, DateTimeOffset.UtcNow,
                new StreamState(200, 2048, null, null, 1, 200))
        };

        var consumers1 = new List<ConsumerInfo>
        {
            new("s1", "c1", null, "All", "Explicit", null, 5, 0, 0, true,
                DateTimeOffset.UtcNow, new ConsumerState(100, 95, 5, 0, 0))
        };
        var consumers2 = new List<ConsumerInfo>
        {
            new("s2", "c2", null, "All", "Explicit", null, 0, 0, 0, false,
                DateTimeOffset.UtcNow, new ConsumerState(200, 200, 0, 0, 0)),
            new("s2", "c3", null, "All", "Explicit", null, 1500, 0, 0, true,
                DateTimeOffset.UtcNow, new ConsumerState(100, 100, 1500, 0, 0))
        };

        _envRepo.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _jsAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(streams);
        _jsAdapter.ListConsumersAsync(envId, "s1", Arg.Any<CancellationToken>()).Returns(consumers1);
        _jsAdapter.ListConsumersAsync(envId, "s2", Arg.Any<CancellationToken>()).Returns(consumers2);
        _kvAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<KvBucketInfo>());

        var outputPort = new TestOutputPort<DashboardSummary>();
        await _handler.ExecuteAsync(new GetDashboardQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.JetStream.StreamCount.ShouldBe(2);
        outputPort.Value!.JetStream.ConsumerCount.ShouldBe(3);
        outputPort.Value!.JetStream.TotalMessages.ShouldBe(300);
        outputPort.Value!.JetStream.TotalBytes.ShouldBe(3072);
        outputPort.Value!.JetStream.UnhealthyConsumers.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ShouldDetectHighPendingAlerts()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://localhost:4222");

        var streams = new List<StreamInfo>
        {
            new("s1", "", [], "Limits", "File", 100, 1024, 1, DateTimeOffset.UtcNow,
                new StreamState(100, 1024, null, null, 1, 100))
        };
        var consumers = new List<ConsumerInfo>
        {
            new("s1", "c1", null, "All", "Explicit", null, 5000, 0, 0, true,
                DateTimeOffset.UtcNow, new ConsumerState(100, 100, 5000, 0, 0))
        };

        _envRepo.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _jsAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(streams);
        _jsAdapter.ListConsumersAsync(envId, "s1", Arg.Any<CancellationToken>()).Returns(consumers);
        _kvAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<KvBucketInfo>());

        var outputPort = new TestOutputPort<DashboardSummary>();
        await _handler.ExecuteAsync(new GetDashboardQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Alerts.Count(a => a.Message.Contains("5000")).ShouldBe(1);
    }

    [Fact]
    public async Task Handle_ShouldAggregateKvSummary()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://localhost:4222");

        var buckets = new List<KvBucketInfo>
        {
            new("config", 5, -1, -1, null, 10, 1024),
            new("cache", 1, -1, -1, null, 50, 4096)
        };

        _envRepo.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _jsAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<StreamInfo>());
        _kvAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(buckets);

        var outputPort = new TestOutputPort<DashboardSummary>();
        await _handler.ExecuteAsync(new GetDashboardQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.KeyValue.BucketCount.ShouldBe(2);
        outputPort.Value!.KeyValue.TotalKeys.ShouldBe(60);
    }

    [Fact]
    public async Task Handle_WhenJetStreamFails_ShouldAddErrorAlert()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://localhost:4222");

        _envRepo.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);
        _jsAdapter.ListStreamsAsync(envId, Arg.Any<CancellationToken>()).ThrowsAsync(new Exception("JetStream down"));
        _kvAdapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(new List<KvBucketInfo>());

        var outputPort = new TestOutputPort<DashboardSummary>();
        await _handler.ExecuteAsync(new GetDashboardQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Alerts.ShouldContain(a => a.Severity == "error" && a.ResourceType == "JetStream");
        outputPort.Value!.JetStream.StreamCount.ShouldBe(0);
    }
}

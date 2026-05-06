using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsManager.Web.BackgroundServices;
using NatsManager.Web.Hubs;
using NSubstitute;
using Shouldly;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.BackgroundServices;

public sealed class MonitoringPollerTests
{
    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenEnvironmentIsDueAgain_ShouldUsePreviousSnapshotForNextPoll()
    {
        var environment = CreateEnvironment("monitored", "http://localhost:8222");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([environment], [environment]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var firstSnapshot = CreateSnapshot(environment.Id);
        var secondSnapshot = CreateSnapshot(environment.Id);
        adapter.FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>())
            .Returns(firstSnapshot);
        adapter.FetchSnapshotAsync(environment, firstSnapshot, Arg.Any<CancellationToken>())
            .Returns(secondSnapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(environment.Id).Returns([firstSnapshot], [firstSnapshot, secondSnapshot]);
        var poller = CreatePoller(repository, adapter, metricsStore, out _);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);
        poller.SetNextPollTime(environment.Id, DateTimeOffset.UtcNow.AddSeconds(-1));

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>());
        await adapter.Received(1).FetchSnapshotAsync(environment, firstSnapshot, Arg.Any<CancellationToken>());
        metricsStore.Received(2).AddSnapshot(Arg.Any<MonitoringSnapshot>());
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenMonitoringUrlIsMissing_ShouldSkipEnvironment()
    {
        var monitored = CreateEnvironment("monitored", "http://localhost:8222");
        var unmonitored = CreateEnvironment("unmonitored");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([monitored, unmonitored]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var snapshot = CreateSnapshot(monitored.Id);
        adapter.FetchSnapshotAsync(monitored, null, Arg.Any<CancellationToken>()).Returns(snapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(monitored.Id).Returns([snapshot]);
        var poller = CreatePoller(repository, adapter, metricsStore, out var clientProxy);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).FetchSnapshotAsync(monitored, null, Arg.Any<CancellationToken>());
        await adapter.DidNotReceive().FetchSnapshotAsync(unmonitored, Arg.Any<MonitoringSnapshot?>(), Arg.Any<CancellationToken>());
        metricsStore.Received(1).AddSnapshot(snapshot);
        await clientProxy.Received(1).SendCoreAsync(
            "ReceiveMonitoringSnapshot",
            Arg.Is<object?[]>(args => ReferenceEquals(args[0], snapshot)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenMonitoringUrlRemainsMissing_ShouldLogOnlyOncePerEnvironment()
    {
        var unmonitored = CreateEnvironment("unmonitored");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([unmonitored], [unmonitored]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        var logger = new TestLogger<MonitoringPoller>();
        var poller = CreatePoller(repository, adapter, metricsStore, out _, logger: logger);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);
        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.DidNotReceive().FetchSnapshotAsync(unmonitored, Arg.Any<MonitoringSnapshot?>(), Arg.Any<CancellationToken>());
        logger.Messages.Count(m => m.Contains("Monitoring URL not configured for 'unmonitored'", StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenPollFails_ShouldContinueWithOtherEnvironments()
    {
        var failing = CreateEnvironment("failing", "http://localhost:8222");
        var succeeding = CreateEnvironment("succeeding", "http://localhost:8223");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([failing, succeeding]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        adapter.FetchSnapshotAsync(failing, null, Arg.Any<CancellationToken>())
            .Returns<MonitoringSnapshot>(_ => throw new InvalidOperationException("boom"));
        var snapshot = CreateSnapshot(succeeding.Id);
        adapter.FetchSnapshotAsync(succeeding, null, Arg.Any<CancellationToken>()).Returns(snapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(succeeding.Id).Returns([snapshot]);
        var poller = CreatePoller(repository, adapter, metricsStore, out var clientProxy);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).FetchSnapshotAsync(failing, null, Arg.Any<CancellationToken>());
        await adapter.Received(1).FetchSnapshotAsync(succeeding, null, Arg.Any<CancellationToken>());
        metricsStore.Received(1).AddSnapshot(snapshot);
        await clientProxy.Received(1).SendCoreAsync(
            "ReceiveMonitoringSnapshot",
            Arg.Is<object?[]>(args => ReferenceEquals(args[0], snapshot)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenIntervalHasNotElapsed_ShouldNotPollAgain()
    {
        var environment = CreateEnvironment("monitored", "http://localhost:8222", pollingIntervalSeconds: 30);
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([environment], [environment]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var snapshot = CreateSnapshot(environment.Id);
        adapter.FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(environment.Id).Returns([snapshot]);
        var poller = CreatePoller(repository, adapter, metricsStore, out _);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);
        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenEnvironmentIntervalIsConfigured_ShouldScheduleUsingOverride()
    {
        var environment = CreateEnvironment("monitored", "http://localhost:8222", pollingIntervalSeconds: 10);
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([environment]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var snapshot = CreateSnapshot(environment.Id);
        adapter.FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(environment.Id).Returns([snapshot]);
        var poller = CreatePoller(repository, adapter, metricsStore, out _);
        var before = DateTimeOffset.UtcNow;

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        poller.TryGetNextPollTime(environment.Id, out var nextPollTime).ShouldBeTrue();
        nextPollTime
            .ShouldBeInRange(before.AddSeconds(8), DateTimeOffset.UtcNow.AddSeconds(12));
    }

    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenEnvironmentIntervalIsNotConfigured_ShouldUseDefaultInterval()
    {
        var environment = CreateEnvironment("monitored", "http://localhost:8222");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([environment]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        var snapshot = CreateSnapshot(environment.Id);
        adapter.FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>())
            .Returns(snapshot);
        var metricsStore = Substitute.For<IMonitoringMetricsStore>();
        metricsStore.GetHistory(environment.Id).Returns([snapshot]);
        var poller = CreatePoller(
            repository,
            adapter,
            metricsStore,
            out _,
            new MonitoringOptions { DefaultPollingIntervalSeconds = 45 });
        var before = DateTimeOffset.UtcNow;

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        poller.TryGetNextPollTime(environment.Id, out var nextPollTime).ShouldBeTrue();
        nextPollTime
            .ShouldBeInRange(before.AddSeconds(43), DateTimeOffset.UtcNow.AddSeconds(47));
    }

    private static MonitoringPoller CreatePoller(
        IEnvironmentRepository repository,
        IMonitoringAdapter adapter,
        IMonitoringMetricsStore metricsStore,
        out IClientProxy clientProxy,
        MonitoringOptions? monitoringOptions = null,
        ILogger<MonitoringPoller>? logger = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var hubContext = Substitute.For<IHubContext<MonitoringHub>>();
        var hubClients = Substitute.For<IHubClients>();
        clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(hubClients);
        hubClients.Group(Arg.Any<string>()).Returns(clientProxy);

        return new MonitoringPoller(
            scopeFactory,
            adapter,
            metricsStore,
            hubContext,
            Options.Create(monitoringOptions ?? new MonitoringOptions()),
            logger ?? NullLogger<MonitoringPoller>.Instance);
    }

    private static Environment CreateEnvironment(
        string name,
        string? monitoringUrl = null,
        int? pollingIntervalSeconds = null)
    {
        var environment = Environment.Create(name, "nats://localhost:4222");
        environment.UpdateMonitoringSettings(monitoringUrl, pollingIntervalSeconds);
        return environment;
    }

    private static MonitoringSnapshot CreateSnapshot(Guid environmentId) =>
        new(
            environmentId,
            DateTimeOffset.UtcNow,
            new ServerMetrics("2.10.0", 1, 1, 100, 1, 1, 1, 1, 0, 0, 0, 0, 1, 1),
            null,
            MonitoringStatus.Ok,
            MonitoringStatus.Ok);

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public static NoOpDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}

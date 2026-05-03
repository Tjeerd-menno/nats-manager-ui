using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
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
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([environment]);
        var adapter = Substitute.For<IMonitoringAdapter>();
        adapter.FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>())
            .Returns(CreateSnapshot(environment.Id));
        var poller = CreatePoller(repository, adapter, Substitute.For<IMonitoringMetricsStore>(), out _);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);
        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).FetchSnapshotAsync(environment, null, Arg.Any<CancellationToken>());
    }

    private static MonitoringPoller CreatePoller(
        IEnvironmentRepository repository,
        IMonitoringAdapter adapter,
        IMonitoringMetricsStore metricsStore,
        out IClientProxy clientProxy)
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
            Options.Create(new MonitoringOptions()),
            NullLogger<MonitoringPoller>.Instance);
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
}

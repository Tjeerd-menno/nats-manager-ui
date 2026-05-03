using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;
using NatsManager.Web.BackgroundServices;
using NSubstitute;
using Shouldly;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Web.Tests.BackgroundServices;

public sealed class ClusterObservationPollerTests
{
    [Fact]
    public async Task PollDueEnvironmentsAsync_WhenAdapterIsScoped_ShouldStoreObservation()
    {
        var monitored = CreateEnvironment("monitored", "http://localhost:8222");
        var unmonitored = CreateEnvironment("unmonitored");
        var repository = Substitute.For<IEnvironmentRepository>();
        repository.GetEnabledAsync(Arg.Any<CancellationToken>()).Returns([monitored, unmonitored]);

        var adapter = Substitute.For<IClusterMonitoringAdapter>();
        var observation = CreateObservation(monitored.Id);
        adapter.GetClusterObservationAsync(monitored.Id, Arg.Any<CancellationToken>()).Returns(observation);

        var store = Substitute.For<IClusterObservationStore>();
        var poller = CreatePoller(repository, adapter, store);

        await poller.PollDueEnvironmentsAsync(CancellationToken.None);

        await adapter.Received(1).GetClusterObservationAsync(monitored.Id, Arg.Any<CancellationToken>());
        await adapter.DidNotReceive().GetClusterObservationAsync(unmonitored.Id, Arg.Any<CancellationToken>());
        store.Received(1).StoreObservation(observation);
    }

    [Fact]
    public void ServiceProviderValidation_WhenClusterMonitoringAdapterIsScoped_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<MonitoringOptions>();
        services.AddScoped<IEnvironmentRepository>(_ => Substitute.For<IEnvironmentRepository>());
        services.AddScoped<IClusterMonitoringAdapter>(_ => Substitute.For<IClusterMonitoringAdapter>());
        services.AddSingleton<IClusterObservationStore>(Substitute.For<IClusterObservationStore>());
        services.AddHostedService<ClusterObservationPoller>();

        Should.NotThrow(() => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        }));
    }

    private static ClusterObservationPoller CreatePoller(
        IEnvironmentRepository repository,
        IClusterMonitoringAdapter adapter,
        IClusterObservationStore store)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => repository);
        services.AddScoped(_ => adapter);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        return new ClusterObservationPoller(
            scopeFactory,
            store,
            Options.Create(new MonitoringOptions()),
            NullLogger<ClusterObservationPoller>.Instance);
    }

    private static Environment CreateEnvironment(string name, string? monitoringUrl = null)
    {
        var environment = Environment.Create(name, "nats://localhost:4222");
        environment.UpdateMonitoringSettings(monitoringUrl, pollingIntervalSeconds: null);
        return environment;
    }

    private static ClusterObservation CreateObservation(Guid environmentId) =>
        new(
            environmentId,
            DateTimeOffset.UtcNow,
            ClusterStatus.Healthy,
            ObservationFreshness.Live,
            ServerCount: 1,
            DegradedServerCount: 0,
            JetStreamAvailable: true,
            ConnectionCount: 5,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            Warnings: [],
            Servers: [],
            Topology: []);
}

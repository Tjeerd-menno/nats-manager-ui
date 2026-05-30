using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;
using NatsEnvironment = NatsManager.Domain.Modules.Environments.Environment;
using NatsManager.Web.Hubs;

namespace NatsManager.Web.BackgroundServices;

public sealed partial class MonitoringPoller(
    IServiceScopeFactory scopeFactory,
    IMonitoringAdapter monitoringAdapter,
    IMonitoringMetricsStore metricsStore,
    IHubContext<MonitoringHub> hubContext,
    IOptions<MonitoringOptions> options,
    ILogger<MonitoringPoller> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, MonitoringSnapshot?> _lastSnapshots = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _nextPollTimes = new();
    private readonly ConcurrentDictionary<Guid, bool> _missingMonitoringUrlLogged = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogPollerStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollDueEnvironmentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogPollerError(ex);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        LogPollerStopped();
    }

    internal async Task PollDueEnvironmentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var environmentRepository = scope.ServiceProvider.GetRequiredService<IEnvironmentRepository>();
        var environments = await environmentRepository.GetEnabledAsync(ct);

        var monitorableEnvironments = new List<NatsEnvironment>();
        foreach (var environment in environments)
        {
            if (environment.MonitoringUrl is null)
            {
                if (_missingMonitoringUrlLogged.TryAdd(environment.Id, true))
                {
                    LogMonitoringUrlNotConfigured(environment.Name);
                }

                continue;
            }

            _missingMonitoringUrlLogged.TryRemove(environment.Id, out _);
            monitorableEnvironments.Add(environment);
        }

        var configuredIds = monitorableEnvironments.Select(e => e.Id).ToHashSet();
        var environmentIds = environments.Select(e => e.Id).ToHashSet();
        foreach (var trackedId in _nextPollTimes.Keys)
        {
            if (!configuredIds.Contains(trackedId))
            {
                _nextPollTimes.TryRemove(trackedId, out _);
                _lastSnapshots.TryRemove(trackedId, out _);
            }
        }

        foreach (var trackedId in _missingMonitoringUrlLogged.Keys)
        {
            if (!environmentIds.Contains(trackedId))
            {
                _missingMonitoringUrlLogged.TryRemove(trackedId, out _);
            }
        }

        var now = DateTimeOffset.UtcNow;
        var pollingTasks = monitorableEnvironments
            .Where(e => !_nextPollTimes.TryGetValue(e.Id, out var nextPoll) || nextPoll <= now)
            .Select(e => PollEnvironmentAndScheduleNextAsync(e, ct));

        await Task.WhenAll(pollingTasks);
    }

    private async Task PollEnvironmentAndScheduleNextAsync(NatsEnvironment environment, CancellationToken ct)
    {
        try
        {
            await PollEnvironmentAsync(environment, ct);
        }
        finally
        {
            var intervalSeconds = environment.MonitoringPollingIntervalSeconds ?? options.Value.DefaultPollingIntervalSeconds;
            _nextPollTimes[environment.Id] = DateTimeOffset.UtcNow.AddSeconds(intervalSeconds);
        }
    }

    private async Task PollEnvironmentAsync(NatsEnvironment environment, CancellationToken ct)
    {
        try
        {
            _lastSnapshots.TryGetValue(environment.Id, out var previous);
            var snapshot = await monitoringAdapter.FetchSnapshotAsync(environment, previous, ct);
            _lastSnapshots[environment.Id] = snapshot;
            metricsStore.AddSnapshot(snapshot);

            await hubContext.Clients
                .Group($"env-{environment.Id}")
                .SendAsync("ReceiveMonitoringSnapshot", snapshot, ct);

            var snapshotCount = metricsStore.GetHistory(environment.Id).Count;
            LogPollCycleComplete(environment.Name, snapshotCount);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogPollCycleFailed(environment.Name, ex);
        }
    }

    internal bool TryGetNextPollTime(Guid environmentId, out DateTimeOffset nextPollTime) =>
        _nextPollTimes.TryGetValue(environmentId, out nextPollTime);

    internal void SetNextPollTime(Guid environmentId, DateTimeOffset nextPollTime) =>
        _nextPollTimes[environmentId] = nextPollTime;

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitoring poller started")]
    private partial void LogPollerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitoring poller stopped")]
    private partial void LogPollerStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Monitoring polling cycle failed")]
    private partial void LogPollerError(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Poll cycle complete for '{EnvName}': {SnapshotCount} snapshots")]
    private partial void LogPollCycleComplete(string envName, int snapshotCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Poll cycle failed for '{EnvName}'")]
    private partial void LogPollCycleFailed(string envName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Monitoring URL not configured for '{EnvName}'")]
    private partial void LogMonitoringUrlNotConfigured(string envName);
}

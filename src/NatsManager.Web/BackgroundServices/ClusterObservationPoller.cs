using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Web.BackgroundServices;

/// <summary>
/// Background service that polls cluster monitoring endpoints for each environment
/// and stores observations in IClusterObservationStore.
/// Isolates per-environment failures — one environment failure does not affect others.
/// </summary>
public sealed partial class ClusterObservationPoller(
    IServiceScopeFactory scopeFactory,
    IClusterMonitoringAdapter clusterMonitoringAdapter,
    IClusterObservationStore observationStore,
    IOptions<MonitoringOptions> options,
    ILogger<ClusterObservationPoller> logger) : BackgroundService
{
    private readonly Dictionary<Guid, DateTimeOffset> _nextPollTimes = new();

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
                LogPollerError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        LogPollerStopped();
    }

    private async Task PollDueEnvironmentsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var environmentRepository = scope.ServiceProvider.GetRequiredService<IEnvironmentRepository>();
        var environments = await environmentRepository.GetEnabledAsync(ct);
        var monitorableEnvironments = environments
            .Where(e => e.MonitoringUrl is not null)
            .ToArray();

        var now = DateTimeOffset.UtcNow;
        var intervalSecs = options.Value.ClusterPollingIntervalSeconds;

        foreach (var env in monitorableEnvironments)
        {
            if (_nextPollTimes.TryGetValue(env.Id, out var next) && now < next)
                continue;

            _nextPollTimes[env.Id] = now.AddSeconds(intervalSecs);

            try
            {
                LogPollingEnvironment(env.Id);
                var observation = await clusterMonitoringAdapter.GetClusterObservationAsync(env.Id, ct);
                observationStore.StoreObservation(observation);
                LogPolledEnvironment(env.Id, observation.Status);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogEnvironmentPollFailed(env.Id, ex.Message);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "ClusterObservationPoller started.")]
    private partial void LogPollerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "ClusterObservationPoller stopped.")]
    private partial void LogPollerStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Polling cluster observation for environment {EnvironmentId}.")]
    private partial void LogPollingEnvironment(Guid environmentId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Polled cluster observation for environment {EnvironmentId}: {Status}.")]
    private partial void LogPolledEnvironment(Guid environmentId, ClusterStatus status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to poll cluster observation for environment {EnvironmentId}: {Reason}.")]
    private partial void LogEnvironmentPollFailed(Guid environmentId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "ClusterObservationPoller error: {Reason}.")]
    private partial void LogPollerError(string reason);
}

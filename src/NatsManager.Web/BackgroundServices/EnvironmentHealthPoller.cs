using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Web.BackgroundServices;

public sealed partial class EnvironmentHealthPoller(
    IServiceScopeFactory scopeFactory,
    INatsHealthChecker healthChecker,
    ILogger<EnvironmentHealthPoller> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogPollerStarted();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollEnvironmentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogPollerError(ex.Message);
            }

            await Task.Delay(DefaultInterval, stoppingToken);
        }

        LogPollerStopped();
    }

    private async Task PollEnvironmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var environmentRepository = scope.ServiceProvider.GetRequiredService<IEnvironmentRepository>();

        var environments = await environmentRepository.GetEnabledAsync(cancellationToken);

        foreach (var environment in environments)
        {
            try
            {
                var result = await healthChecker.CheckHealthAsync(environment, cancellationToken);

                var newStatus = result.Reachable
                    ? Domain.Modules.Common.ConnectionStatus.Available
                    : Domain.Modules.Common.ConnectionStatus.Unavailable;

                if (environment.ConnectionStatus != newStatus)
                {
                    environment.UpdateConnectionStatus(newStatus);
                    await environmentRepository.UpdateAsync(environment, cancellationToken);
                    var statusName = newStatus.ToString();
                    LogStatusChanged(environment.Name, statusName);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogEnvironmentPollFailed(environment.Name, ex.Message);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment health poller started")]
    private partial void LogPollerStarted();

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment health poller stopped")]
    private partial void LogPollerStopped();

    [LoggerMessage(Level = LogLevel.Error, Message = "Environment health polling cycle failed: {Error}")]
    private partial void LogPollerError(string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Environment '{Name}' status changed to {Status}")]
    private partial void LogStatusChanged(string name, string status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to poll environment '{Name}': {Error}")]
    private partial void LogEnvironmentPollFailed(string name, string error);
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring.Ports;

namespace NatsManager.Web.Hubs;

[Authorize]
public sealed class MonitoringHub(
    IEnvironmentRepository environmentRepository,
    IMonitoringMetricsStore metricsStore) : Hub
{
    public async Task SubscribeToEnvironment(string environmentId)
    {
        if (!Guid.TryParse(environmentId, out var id))
            throw new HubException("Invalid environment id.");

        var environment = await environmentRepository.GetByIdAsync(id, Context.ConnectionAborted);
        if (environment is null)
            throw new HubException("Environment not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"env-{id}", Context.ConnectionAborted);

        var latest = metricsStore.GetLatest(id);
        if (latest is not null)
            await Clients.Caller.SendAsync("ReceiveMonitoringSnapshot", latest, Context.ConnectionAborted);
    }

    public Task UnsubscribeFromEnvironment(string environmentId) =>
        Guid.TryParse(environmentId, out var id)
            ? Groups.RemoveFromGroupAsync(Context.ConnectionId, $"env-{id}", Context.ConnectionAborted)
            : Task.CompletedTask;
}

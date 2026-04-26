using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NatsManager.Web.Hubs;

[Authorize]
public sealed class MonitoringHub : Hub
{
    public Task SubscribeToEnvironment(string environmentId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"env-{environmentId}");

    public Task UnsubscribeFromEnvironment(string environmentId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"env-{environmentId}");
}

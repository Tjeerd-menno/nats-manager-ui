using NatsManager.Application.Modules.Monitoring.Models;

namespace NatsManager.Application.Modules.Monitoring.Ports;

public interface IMonitoringAdapter
{
    Task<MonitoringSnapshot> FetchSnapshotAsync(
        Domain.Modules.Environments.Environment environment,
        MonitoringSnapshot? previous,
        CancellationToken ct);
}

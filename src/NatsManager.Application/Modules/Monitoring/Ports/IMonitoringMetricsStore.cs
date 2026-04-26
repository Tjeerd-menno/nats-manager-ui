using NatsManager.Application.Modules.Monitoring.Models;

namespace NatsManager.Application.Modules.Monitoring.Ports;

public interface IMonitoringMetricsStore
{
    void AddSnapshot(MonitoringSnapshot snapshot);
    IReadOnlyList<MonitoringSnapshot> GetHistory(Guid environmentId);
    MonitoringSnapshot? GetLatest(Guid environmentId);
}

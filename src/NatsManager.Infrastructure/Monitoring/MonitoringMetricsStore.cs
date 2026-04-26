using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;

namespace NatsManager.Infrastructure.Monitoring;

public sealed class MonitoringMetricsStore(IOptions<MonitoringOptions> options) : IMonitoringMetricsStore
{
    private readonly ConcurrentDictionary<Guid, EnvironmentMetricsBuffer> _store = new();

    public void AddSnapshot(MonitoringSnapshot snapshot)
    {
        var buffer = _store.GetOrAdd(snapshot.EnvironmentId,
            _ => new EnvironmentMetricsBuffer(options.Value.MaxSnapshotsPerEnvironment));
        buffer.Add(snapshot);
    }

    public IReadOnlyList<MonitoringSnapshot> GetHistory(Guid environmentId) =>
        _store.TryGetValue(environmentId, out var buffer)
            ? buffer.GetAll()
            : Array.Empty<MonitoringSnapshot>();

    public MonitoringSnapshot? GetLatest(Guid environmentId) =>
        _store.TryGetValue(environmentId, out var buffer) ? buffer.GetLatest() : null;
}

internal sealed class EnvironmentMetricsBuffer(int maxCapacity)
{
    private readonly Queue<MonitoringSnapshot> _queue = new();
    private readonly object _lock = new();

    public void Add(MonitoringSnapshot snapshot)
    {
        lock (_lock)
        {
            while (_queue.Count >= maxCapacity)
                _queue.Dequeue();
            _queue.Enqueue(snapshot);
        }
    }

    public IReadOnlyList<MonitoringSnapshot> GetAll()
    {
        lock (_lock)
            return [.. _queue];
    }

    public MonitoringSnapshot? GetLatest()
    {
        lock (_lock)
            return _queue.Count > 0 ? _queue.Last() : null;
    }
}

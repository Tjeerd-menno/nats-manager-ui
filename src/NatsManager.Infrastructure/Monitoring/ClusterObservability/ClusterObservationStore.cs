using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Infrastructure.Monitoring.ClusterObservability;

/// <summary>
/// In-memory store for cluster observations with a bounded ring buffer per environment.
/// Freshness transitions are managed here per the data-model state machine.
/// No payload, JWT, or credential data is retained.
/// </summary>
public sealed class ClusterObservationStore(IOptions<MonitoringOptions> options) : IClusterObservationStore
{
    private readonly ConcurrentDictionary<Guid, EnvironmentObservationBuffer> _store = new();

    public ClusterObservation? GetLatest(Guid environmentId) =>
        _store.TryGetValue(environmentId, out var buffer) ? buffer.GetLatest() : null;

    public void StoreObservation(ClusterObservation observation)
    {
        var buffer = _store.GetOrAdd(observation.EnvironmentId,
            _ => new EnvironmentObservationBuffer(options.Value.MaxRetainedObservations));
        buffer.Add(observation);
    }

    public IReadOnlyList<ClusterObservation> GetRetained(Guid environmentId) =>
        _store.TryGetValue(environmentId, out var buffer)
            ? buffer.GetAll()
            : [];
}

internal sealed class EnvironmentObservationBuffer(int maxCapacity)
{
    private readonly Queue<ClusterObservation> _queue = new();
    private readonly Lock _lock = new();
    private ClusterObservation? _latest;

    public void Add(ClusterObservation observation)
    {
        lock (_lock)
        {
            while (_queue.Count >= maxCapacity)
                _queue.Dequeue();
            _queue.Enqueue(observation);
            _latest = observation;
        }
    }

    public IReadOnlyList<ClusterObservation> GetAll()
    {
        lock (_lock)
            return [.. _queue];
    }

    public ClusterObservation? GetLatest()
    {
        lock (_lock)
            return _latest;
    }
}

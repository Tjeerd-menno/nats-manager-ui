using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Infrastructure.Monitoring;
using Shouldly;

namespace NatsManager.Infrastructure.Tests.Monitoring;

public sealed class MonitoringMetricsStoreTests
{
    [Fact]
    public void AddSnapshot_WhenCapacityExceeded_ShouldKeepLatestSnapshotsInOrder()
    {
        var store = new MonitoringMetricsStore(Options.Create(new MonitoringOptions
        {
            MaxSnapshotsPerEnvironment = 2
        }));
        var environmentId = Guid.NewGuid();
        var first = CreateSnapshot(environmentId, 1);
        var second = CreateSnapshot(environmentId, 2);
        var third = CreateSnapshot(environmentId, 3);

        store.AddSnapshot(first);
        store.AddSnapshot(second);
        store.AddSnapshot(third);

        var history = store.GetHistory(environmentId);
        history.Count.ShouldBe(2);
        history[0].Timestamp.ShouldBe(second.Timestamp);
        history[1].Timestamp.ShouldBe(third.Timestamp);
        store.GetLatest(environmentId)!.Timestamp.ShouldBe(third.Timestamp);
    }

    private static MonitoringSnapshot CreateSnapshot(Guid environmentId, int offsetSeconds) =>
        new(
            environmentId,
            DateTimeOffset.UtcNow.AddSeconds(offsetSeconds),
            new ServerMetrics("", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            null,
            MonitoringStatus.Ok,
            MonitoringStatus.Ok);
}

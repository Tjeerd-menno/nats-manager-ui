import { useMemo } from 'react';
import { useRecentMonitoringSnapshots } from '../hooks/use-recent-monitoring-snapshots';

export function useDashboardTraffic(environmentId: string | null) {
  const snapshots = useRecentMonitoringSnapshots(environmentId, 120);

  return useMemo(() => {
    const latest = snapshots.data[0] ?? null;
    const previous = snapshots.data[1] ?? null;

    return {
      inMessagesPerSecond: latest?.inMessagesPerSecond ?? 0,
      outMessagesPerSecond: latest?.outMessagesPerSecond ?? 0,
      inBytesPerSecond: latest?.inBytesPerSecond ?? 0,
      outBytesPerSecond: latest?.outBytesPerSecond ?? 0,
      inMessagesTrend: latest && previous ? latest.inMessagesPerSecond - previous.inMessagesPerSecond : 0,
      outMessagesTrend: latest && previous ? latest.outMessagesPerSecond - previous.outMessagesPerSecond : 0,
    };
  }, [snapshots.data]);
}

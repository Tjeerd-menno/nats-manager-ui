import { useMemo } from 'react';
import { useLatestMonitoringSnapshot } from '../hooks/use-latest-monitoring-snapshot';

export function useDashboardJetStream(environmentId: string | null) {
  const latest = useLatestMonitoringSnapshot(environmentId);

  return useMemo(() => ({
    isEnabled: latest.data?.jetStreamEnabled ?? null,
    streamCount: latest.data?.streamCount ?? 0,
    consumerCount: latest.data?.consumerCount ?? 0,
    totalMessages: latest.data?.totalMessages ?? 0,
    totalBytes: latest.data?.totalBytes ?? 0,
  }), [latest.data]);
}

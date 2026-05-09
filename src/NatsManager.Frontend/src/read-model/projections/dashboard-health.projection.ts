import { useMemo } from 'react';
import { useLatestMonitoringSnapshot } from '../hooks/use-latest-monitoring-snapshot';
import { useMonitoringEnvironmentState } from '../hooks/use-monitoring-environment-state';

export function useDashboardHealth(environmentId: string | null) {
  const latest = useLatestMonitoringSnapshot(environmentId);
  const state = useMonitoringEnvironmentState(environmentId);

  return useMemo(() => ({
    healthStatus: state.data?.healthStatus ?? latest.data?.healthStatus ?? 'Unknown',
    connectionStatus: state.data?.connectionStatus ?? latest.data?.connectionStatus ?? 'Disconnected',
    latestSnapshotAgeMs: null,
    lastUpdatedUtc: state.data?.lastReceivedAtUtc ?? latest.data?.receivedAtUtc ?? null,
    errorMessage: state.data?.errorMessage ?? null,
    isNotConfigured: state.data?.isNotConfigured ?? false,
  }), [latest.data, state.data]);
}

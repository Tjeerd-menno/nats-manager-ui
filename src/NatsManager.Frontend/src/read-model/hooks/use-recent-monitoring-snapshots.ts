import { useMemo } from 'react';
import { useLiveQuery } from '@tanstack/react-db';
import { monitoringSnapshotsCollection } from '../collections';
import { READ_MODEL_UNKNOWN_ENVIRONMENT_ID } from '../types/common';

export function useRecentMonitoringSnapshots(environmentId: string | null, limit = 120) {
  const { data = [], ...query } = useLiveQuery((q) => q.from({ snapshot: monitoringSnapshotsCollection }), []);
  const scopedEnvironmentId = environmentId ?? READ_MODEL_UNKNOWN_ENVIRONMENT_ID;

  const snapshots = useMemo(
    () =>
      [...data]
        .filter(snapshot => snapshot.environmentId === scopedEnvironmentId)
        .sort((left, right) => Date.parse(right.observedAtUtc) - Date.parse(left.observedAtUtc))
        .slice(0, limit),
    [data, limit, scopedEnvironmentId],
  );

  return { ...query, data: snapshots };
}

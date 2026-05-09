import { useMemo } from 'react';
import { useLiveQuery } from '@tanstack/react-db';
import { monitoringSnapshotsCollection } from '../collections';
import { READ_MODEL_UNKNOWN_ENVIRONMENT_ID } from '../types/common';

export function useLatestMonitoringSnapshot(environmentId: string | null) {
  const { data = [], ...query } = useLiveQuery((q) => q.from({ snapshot: monitoringSnapshotsCollection }), []);
  const scopedEnvironmentId = environmentId ?? READ_MODEL_UNKNOWN_ENVIRONMENT_ID;

  const latestSnapshot = useMemo(
    () =>
      [...data]
        .filter(snapshot => snapshot.environmentId === scopedEnvironmentId)
        .sort((left, right) => Date.parse(right.observedAtUtc) - Date.parse(left.observedAtUtc))[0] ?? null,
    [data, scopedEnvironmentId],
  );

  return { ...query, data: latestSnapshot };
}

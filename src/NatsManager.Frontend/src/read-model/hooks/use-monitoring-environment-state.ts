import { useMemo } from 'react';
import { useLiveQuery } from '@tanstack/react-db';
import { monitoringEnvironmentStateCollection } from '../collections';

export function useMonitoringEnvironmentState(environmentId: string | null) {
  const { data = [], ...query } = useLiveQuery((q) => q.from({ state: monitoringEnvironmentStateCollection }), []);

  const state = useMemo(
    () => data.find(item => item.environmentId === environmentId) ?? null,
    [data, environmentId],
  );

  return { ...query, data: state };
}

import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { MonitoringEnvironmentStateRecord } from '../types/monitoring-read-model';

export const monitoringEnvironmentStateCollection = createCollection<MonitoringEnvironmentStateRecord, string>(
  localOnlyCollectionOptions({
    id: 'monitoring-environment-state',
    getKey: record => record.environmentId,
  }),
);

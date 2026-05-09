import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { MonitoringSnapshotRecord } from '../types/monitoring-read-model';

export const monitoringSnapshotsCollection = createCollection<MonitoringSnapshotRecord, string>(
  localOnlyCollectionOptions({
    id: 'monitoring-snapshots',
    getKey: record => record.id,
  }),
);

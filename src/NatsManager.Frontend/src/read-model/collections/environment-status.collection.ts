import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { EnvironmentStatusRecord } from '../types/environment-read-model';

export const environmentStatusCollection = createCollection<EnvironmentStatusRecord, string>(
  localOnlyCollectionOptions({
    id: 'environment-status',
    getKey: record => record.environmentId,
  }),
);

import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { StreamRecord } from '../types/jetstream-read-model';

export const streamsCollection = createCollection<StreamRecord, string>(
  localOnlyCollectionOptions({
    id: 'jetstream-streams',
    getKey: record => record.id,
  }),
);

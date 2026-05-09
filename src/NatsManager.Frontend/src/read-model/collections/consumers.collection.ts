import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { ConsumerRecord } from '../types/jetstream-read-model';

export const consumersCollection = createCollection<ConsumerRecord, string>(
  localOnlyCollectionOptions({
    id: 'jetstream-consumers',
    getKey: record => record.id,
  }),
);

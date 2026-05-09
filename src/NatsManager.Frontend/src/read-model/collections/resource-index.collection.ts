import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { ResourceIndexItemRecord } from '../types/resource-index-read-model';

export const resourceIndexCollection = createCollection<ResourceIndexItemRecord, string>(
  localOnlyCollectionOptions({
    id: 'resource-index',
    getKey: record => record.id,
  }),
);

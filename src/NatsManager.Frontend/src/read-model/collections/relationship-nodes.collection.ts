import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { RelationshipNodeRecord } from '../types/relationship-read-model';

export const relationshipNodesCollection = createCollection<RelationshipNodeRecord, string>(
  localOnlyCollectionOptions({
    id: 'relationship-nodes',
    getKey: record => record.id,
  }),
);

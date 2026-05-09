import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { RelationshipEdgeRecord } from '../types/relationship-read-model';

export const relationshipEdgesCollection = createCollection<RelationshipEdgeRecord, string>(
  localOnlyCollectionOptions({
    id: 'relationship-edges',
    getKey: record => record.id,
  }),
);

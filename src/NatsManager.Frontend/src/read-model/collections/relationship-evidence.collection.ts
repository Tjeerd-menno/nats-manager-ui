import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { RelationshipEvidenceRecord } from '../types/relationship-read-model';

export const relationshipEvidenceCollection = createCollection<RelationshipEvidenceRecord, string>(
  localOnlyCollectionOptions({
    id: 'relationship-evidence',
    getKey: record => record.id,
  }),
);

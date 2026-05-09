import { createCollection, localOnlyCollectionOptions } from '@tanstack/react-db';
import type { SelectedEnvironmentRecord } from '../types/environment-read-model';

const initialSelectedEnvironment: SelectedEnvironmentRecord = {
  id: 'selected-environment',
  environmentId: null,
  selectedAtUtc: null,
};

export const selectedEnvironmentCollection = createCollection<SelectedEnvironmentRecord, string>(
  localOnlyCollectionOptions<SelectedEnvironmentRecord, string>({
    id: 'selected-environment',
    getKey: record => record.id,
    initialData: [initialSelectedEnvironment],
  }),
);

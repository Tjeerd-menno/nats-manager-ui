import { environmentStatusCollection, resourceIndexCollection, selectedEnvironmentCollection } from '../collections';
import type { EnvironmentDetail, EnvironmentListItem } from '../../features/environments/types';
import type { EnvironmentStatusRecord } from '../types/environment-read-model';
import { calculateFreshness, makeEnvironmentScopedId, upsertRecord } from '../utils/collection-utils';

type EnvironmentSource = EnvironmentListItem | EnvironmentDetail;

export function syncEnvironmentStatus(environment: EnvironmentSource, observedAtUtc = new Date().toISOString()) {
  const freshness = environment.lastSuccessfulContact
    ? calculateFreshness(environment.lastSuccessfulContact)
    : 'Unknown';
  const record: EnvironmentStatusRecord = {
    environmentId: environment.id,
    displayName: environment.name,
    connectionStatus: environment.connectionStatus,
    freshness,
    lastSuccessfulInteractionUtc: environment.lastSuccessfulContact,
    lastCheckedAtUtc: observedAtUtc,
    lastErrorMessage: environment.connectionStatus === 'Unavailable' ? 'Environment unavailable' : null,
  };

  upsertRecord(environmentStatusCollection, record.environmentId, record);

  upsertRecord(resourceIndexCollection, makeEnvironmentScopedId(environment.id, 'Environment', environment.id), {
    id: makeEnvironmentScopedId(environment.id, 'Environment', environment.id),
    environmentId: environment.id,
    resourceType: 'Environment',
    resourceId: environment.id,
    name: environment.name,
    displayName: environment.name,
    description: environment.description || null,
    status: toEnvironmentHealthStatus(environment.connectionStatus),
    freshness: record.freshness,
    detailRoute: `/environments/${environment.id}`,
    searchableText: [environment.name, environment.description, environment.connectionStatus].filter(Boolean).join(' ').toLowerCase(),
    lastObservedAtUtc: environment.lastSuccessfulContact,
    indexedAtUtc: observedAtUtc,
  });
}

export function syncEnvironmentStatuses(environments: EnvironmentSource[], observedAtUtc = new Date().toISOString()) {
  for (const environment of environments) {
    syncEnvironmentStatus(environment, observedAtUtc);
  }
}

export function writeSelectedEnvironment(environmentId: string | null, selectedAtUtc = new Date().toISOString()) {
  selectedEnvironmentCollection.update('selected-environment', draft => {
    draft.environmentId = environmentId;
    draft.selectedAtUtc = environmentId ? selectedAtUtc : null;
  });
}

function toEnvironmentHealthStatus(connectionStatus: EnvironmentSource['connectionStatus']) {
  if (connectionStatus === 'Available') return 'Ok';
  if (connectionStatus === 'Degraded') return 'Degraded';
  if (connectionStatus === 'Unavailable') return 'Unavailable';
  return 'Unknown';
}

import { resourceIndexCollection } from '../collections';
import type { SearchResult } from '../../features/search/types';
import { makeEnvironmentScopedId, upsertRecord } from '../utils/collection-utils';
import type { ResourceIndexType } from '../types/resource-index-read-model';

export function syncSearchResults(results: SearchResult[], indexedAtUtc = new Date().toISOString()) {
  for (const result of results) {
    const environmentId = result.environmentId ?? 'global';
    const resourceType = toResourceIndexType(result.resourceType);
    const id = makeEnvironmentScopedId(environmentId, resourceType, result.resourceId);

    upsertRecord(resourceIndexCollection, id, {
      id,
      environmentId,
      resourceType,
      resourceId: result.resourceId,
      name: result.displayName,
      displayName: result.displayName,
      description: result.description ?? null,
      status: 'Unknown',
      freshness: 'Unknown',
      detailRoute: null,
      searchableText: [result.displayName, result.description, result.resourceType, result.environmentName].filter(Boolean).join(' ').toLowerCase(),
      lastObservedAtUtc: null,
      indexedAtUtc,
    });
  }
}

function toResourceIndexType(resourceType: string): ResourceIndexType {
  const knownTypes = new Set<ResourceIndexType>([
    'Environment',
    'Server',
    'Connection',
    'Subject',
    'Stream',
    'Consumer',
    'KvBucket',
    'KvKey',
    'ObjectBucket',
    'Object',
    'ObjectStoreObject',
    'Service',
    'Endpoint',
    'ServiceEndpoint',
    'Alert',
    'Event',
    'External',
    'JetStreamAccount',
    'Client',
    'RelationshipNode',
  ]);

  return knownTypes.has(resourceType as ResourceIndexType) ? resourceType as ResourceIndexType : 'External';
}

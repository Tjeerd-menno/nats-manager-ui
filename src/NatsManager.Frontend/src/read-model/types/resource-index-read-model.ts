import type { Freshness, HealthStatus } from './common';

export type ResourceIndexType =
  | 'Environment'
  | 'Server'
  | 'Connection'
  | 'Subject'
  | 'Stream'
  | 'Consumer'
  | 'KvBucket'
  | 'KvKey'
  | 'ObjectBucket'
  | 'Object'
  | 'ObjectItem'
  | 'ObjectStoreObject'
  | 'Service'
  | 'Endpoint'
  | 'ServiceEndpoint'
  | 'Alert'
  | 'Event'
  | 'External'
  | 'JetStreamAccount'
  | 'Client'
  | 'RelationshipNode';

export interface ResourceIndexItemRecord {
  id: string;
  environmentId: string;
  resourceType: ResourceIndexType;
  resourceId: string;
  name: string;
  displayName: string;
  description: string | null;
  status: HealthStatus;
  freshness: Freshness;
  detailRoute: string | null;
  searchableText: string;
  lastObservedAtUtc: string | null;
  indexedAtUtc: string;
}

export interface GlobalResourceSearchOptions {
  query: string;
  environmentId?: string | null;
  resourceType?: string | null;
  status?: HealthStatus | null;
  freshness?: Freshness | null;
  sortBy?: 'relevance' | 'resourceType' | 'name';
}

export type ResourceType =
  | 'Server'
  | 'Subject'
  | 'Stream'
  | 'Consumer'
  | 'KvBucket'
  | 'KvKey'
  | 'ObjectBucket'
  | 'Object'
  | 'ObjectStoreObject'
  | 'Service'
  | 'Endpoint'
  | 'ServiceEndpoint'
  | 'Alert'
  | 'Event'
  | 'External'
  | 'JetStreamAccount'
  | 'Client';

export type RelationshipType =
  | 'Contains'
  | 'ConsumesFrom'
  | 'PublishesTo'
  | 'SubscribesTo'
  | 'UsesSubject'
  | 'BackedByStream'
  | 'HostedOn'
  | 'RoutedThrough'
  | 'HostsJetStream'
  | 'AffectedBy'
  | 'RelatedEvent'
  | 'DependsOn'
  | 'ExternalReference';

export type RelationshipDirection = 'Inbound' | 'Outbound' | 'Bidirectional' | 'Unknown';
export type ObservationKind = 'Observed' | 'Inferred';
export type RelationshipConfidence = 'High' | 'Medium' | 'Low' | 'Unknown';
export type RelationshipFreshness = 'Live' | 'Stale' | 'Partial' | 'Unavailable';
export type ResourceHealthStatus = 'Healthy' | 'Warning' | 'Degraded' | 'Stale' | 'Unavailable' | 'Unknown';
export type RelationshipSourceModule =
  | 'CoreNats'
  | 'JetStream'
  | 'KeyValue'
  | 'ObjectStore'
  | 'Services'
  | 'Monitoring'
  | 'Alerts'
  | 'Events'
  | 'Search';

export interface RelationshipEvidence {
  sourceModule: RelationshipSourceModule;
  evidenceType: string;
  observedAt: string;
  freshness: RelationshipFreshness;
  summary: string;
  safeFields: Record<string, string>;
}

export interface ResourceNode {
  nodeId: string;
  environmentId: string;
  resourceType: ResourceType;
  resourceId: string;
  displayName: string;
  status: ResourceHealthStatus;
  freshness: RelationshipFreshness;
  isFocal: boolean;
  detailRoute: string | null;
  metadata: Record<string, string>;
}

export interface RelationshipEdge {
  edgeId: string;
  environmentId: string;
  sourceNodeId: string;
  targetNodeId: string;
  relationshipType: RelationshipType;
  direction: RelationshipDirection;
  observationKind: ObservationKind;
  confidence: RelationshipConfidence;
  freshness: RelationshipFreshness;
  status: ResourceHealthStatus;
  evidence: RelationshipEvidence[];
}

export interface OmittedCounts {
  filteredNodes: number;
  filteredEdges: number;
  collapsedNodes: number;
  collapsedEdges: number;
  unsafeRelationships: number;
}

export interface FocalResource {
  environmentId: string;
  resourceType: ResourceType;
  resourceId: string;
  displayName: string;
  route: string | null;
}

export interface MapFilter {
  depth: number;
  resourceTypes: ResourceType[] | null;
  relationshipTypes: RelationshipType[] | null;
  healthStates: ResourceHealthStatus[] | null;
  minimumConfidence: RelationshipConfidence;
  includeInferred: boolean;
  includeStale: boolean;
  maxNodes: number;
  maxEdges: number;
}

export interface RelationshipMap {
  environmentId: string;
  focalResource: FocalResource;
  generatedAt: string;
  depth: number;
  nodes: ResourceNode[];
  edges: RelationshipEdge[];
  filters: MapFilter;
  omittedCounts: OmittedCounts;
}

export interface MapFilterParams {
  type: ResourceType;
  id: string;
  depth?: number;
  maxNodes?: number;
  maxEdges?: number;
  minConfidence?: RelationshipConfidence;
}

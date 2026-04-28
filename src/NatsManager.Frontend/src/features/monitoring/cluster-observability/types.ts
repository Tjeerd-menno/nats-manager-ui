export type ClusterStatus = 'Healthy' | 'Degraded' | 'Unavailable' | 'Unknown';
export type ServerStatus = 'Healthy' | 'Warning' | 'Stale' | 'Unavailable' | 'Unknown';
export type RelationshipStatus = 'Healthy' | 'Warning' | 'Stale' | 'Unavailable' | 'Unknown';
export type ObservationFreshness = 'Live' | 'Stale' | 'Partial' | 'Unavailable';
export type MetricState = 'Live' | 'Derived' | 'Stale' | 'Unavailable';
export type TopologyRelationshipType = 'Route' | 'Gateway' | 'LeafNode' | 'ClusterPeer';
export type RelationshipDirectionType = 'Inbound' | 'Outbound' | 'Bidirectional' | 'Unknown';
export type MonitoringEndpoint = 'Healthz' | 'Varz' | 'Jsz' | 'Routez' | 'Gatewayz' | 'Leafz' | 'Other';

export interface ClusterWarning {
  code: string;
  severity: 'Info' | 'Warning' | 'Critical';
  message: string;
  serverId: string | null;
  metricName: string | null;
  currentValue: number | null;
  thresholdValue: number | null;
}

export interface ServerObservation {
  environmentId: string;
  serverId: string;
  serverName: string | null;
  clusterName: string | null;
  version: string | null;
  uptimeSeconds: number | null;
  status: ServerStatus;
  freshness: ObservationFreshness;
  connections: number | null;
  maxConnections: number | null;
  slowConsumers: number | null;
  memoryBytes: number | null;
  storageBytes: number | null;
  inMsgsPerSecond: number | null;
  outMsgsPerSecond: number | null;
  inBytesPerSecond: number | null;
  outBytesPerSecond: number | null;
  lastObservedAt: string;
  metricStates: MetricState[];
}

export interface TopologyRelationship {
  environmentId: string;
  relationshipId: string;
  sourceNodeId: string;
  targetNodeId: string;
  type: TopologyRelationshipType;
  direction: RelationshipDirectionType;
  status: RelationshipStatus;
  freshness: ObservationFreshness;
  observedAt: string;
  sourceEndpoint: MonitoringEndpoint;
  safeLabel: string;
}

export interface ClusterObservation {
  environmentId: string;
  observedAt: string;
  status: ClusterStatus;
  freshness: ObservationFreshness;
  serverCount: number;
  degradedServerCount: number;
  jetStreamAvailable: boolean | null;
  connectionCount: number | null;
  inMsgsPerSecond: number | null;
  outMsgsPerSecond: number | null;
  servers: ServerObservation[];
  topology: TopologyRelationship[];
  warnings: ClusterWarning[];
}

export interface ClusterTopologyNode {
  id: string;
  type: string;
  label: string;
  status: string;
  serverId: string | null;
  metadata: Record<string, unknown>;
}

export interface ClusterTopologyOmittedCounts {
  filteredNodes: number;
  filteredEdges: number;
  unsafeRelationships: number;
}

export interface ClusterTopologyGraph {
  environmentId: string;
  observedAt: string;
  freshness: ObservationFreshness;
  nodes: ClusterTopologyNode[];
  edges: TopologyRelationship[];
  omittedCounts: ClusterTopologyOmittedCounts;
}

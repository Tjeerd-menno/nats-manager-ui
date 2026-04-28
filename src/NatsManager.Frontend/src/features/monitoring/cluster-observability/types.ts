export type ClusterStatus = 'Healthy' | 'Degraded' | 'Unavailable' | 'Unknown';
export type ServerStatus = 'Healthy' | 'Warning' | 'Degraded' | 'Unreachable' | 'Unknown';
export type ObservationFreshness = 'Live' | 'Stale' | 'Unavailable';
export type MetricState = 'Normal' | 'Warning' | 'Critical' | 'Unknown';
export type TopologyRelationshipType = 'Route' | 'Gateway' | 'LeafNode' | 'ClusterPeer';
export type RelationshipDirectionType = 'Inbound' | 'Outbound' | 'Bidirectional' | 'Unknown';

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
  serverId: string;
  serverName: string;
  host: string;
  clusterId: string;
  clusterName: string | null;
  version: string | null;
  observedAt: string;
  status: ServerStatus;
  isJetStreamEnabled: boolean;
  connections: number | null;
  slowConsumers: number | null;
  inMsgsPerSec: number | null;
  outMsgsPerSec: number | null;
  inBytesPerSec: number | null;
  outBytesPerSec: number | null;
  memoryBytes: number | null;
  uptimeSeconds: number | null;
  activeAccounts: number | null;
  totalStreams: number | null;
  totalConsumers: number | null;
  jetStreamStorageUsedBytes: number | null;
  jetStreamStorageLimitBytes: number | null;
}

export interface TopologyRelationship {
  relationshipType: TopologyRelationshipType;
  direction: RelationshipDirectionType;
  sourceNodeId: string;
  targetNodeId: string;
  sourceLabel: string;
  targetLabel: string;
  status: 'Active' | 'Inactive' | 'Unknown';
  metadata: Record<string, string>;
}

export interface ClusterObservation {
  environmentId: string;
  observedAt: string;
  status: ClusterStatus;
  freshness: ObservationFreshness;
  serverCount: number;
  degradedServerCount: number;
  isJetStreamEnabled: boolean;
  totalConnections: number;
  totalInMsgsPerSec: number;
  totalOutMsgsPerSec: number;
  servers: ServerObservation[];
  topologyRelationships: TopologyRelationship[];
  warnings: ClusterWarning[];
}

export interface ClusterTopologyNode {
  nodeId: string;
  nodeType: 'server' | 'gateway' | 'leafnode' | 'routePeer' | 'external';
  label: string;
  status: ClusterStatus;
  metadata: Record<string, string>;
}

export interface ClusterTopologyEdge {
  edgeId: string;
  sourceNodeId: string;
  targetNodeId: string;
  relationshipType: TopologyRelationshipType;
  direction: RelationshipDirectionType;
  status: 'Active' | 'Inactive' | 'Unknown';
}

export interface ClusterTopologyGraph {
  environmentId: string;
  observedAt: string;
  freshness: ObservationFreshness;
  nodes: ClusterTopologyNode[];
  edges: ClusterTopologyEdge[];
  omittedCounts: { filteredNodes: number; filteredEdges: number; unavailableServers: number };
}

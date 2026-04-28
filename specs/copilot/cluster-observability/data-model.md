# Data Model: Cluster Observability

**Branch**: `copilot/cluster-observability`  
**Date**: 2026-04-28

---

## 1. `ClusterObservation`

A point-in-time environment-scoped summary of cluster health and freshness.

| Field | Type | Description |
|-------|------|-------------|
| `EnvironmentId` | `Guid` | Selected environment. Required on every observation. |
| `ObservedAt` | `DateTimeOffset` | UTC timestamp when the backend completed the observation. |
| `Status` | `ClusterStatus` | `Healthy`, `Degraded`, `Unavailable`, or `Unknown`. |
| `Freshness` | `ObservationFreshness` | `Live`, `Stale`, `Partial`, or `Unavailable`. |
| `ServerCount` | `int` | Count of observed servers. |
| `DegradedServerCount` | `int` | Servers with warning or unavailable state. |
| `JetStreamAvailable` | `bool?` | `null` when `/jsz` is unavailable or not enabled. |
| `ConnectionCount` | `int?` | Aggregate current connections when available. |
| `InMsgsPerSecond` | `double?` | Derived aggregate inbound message rate. |
| `OutMsgsPerSecond` | `double?` | Derived aggregate outbound message rate. |
| `Warnings` | `ClusterWarning[]` | Derived warnings shown in overview. |
| `Servers` | `ServerObservation[]` | Server-level observations. |
| `Topology` | `TopologyRelationship[]` | Safe route/gateway/leaf/peer relationships. |

**Invariants**:
- `EnvironmentId` must match the selected environment and all nested observations.
- Payload content, account JWTs, and operator JWTs are never included.
- Missing endpoint responses produce `Partial` or `Unavailable`, not fabricated metrics.

---

## 2. `ServerObservation`

Metrics and status for one observed NATS server.

| Field | Type | Description |
|-------|------|-------------|
| `EnvironmentId` | `Guid` | Environment scope. |
| `ServerId` | `string` | Stable server identifier from monitoring metadata when available. |
| `ServerName` | `string?` | Human-readable server name when available. |
| `ClusterName` | `string?` | Cluster name from safe metadata. |
| `Version` | `string?` | NATS version. |
| `UptimeSeconds` | `long?` | Parsed uptime. |
| `Status` | `ServerStatus` | `Healthy`, `Warning`, `Stale`, `Unavailable`, or `Unknown`. |
| `Freshness` | `ObservationFreshness` | Metric freshness. |
| `Connections` | `int?` | Current connections. |
| `MaxConnections` | `int?` | Configured maximum connections. |
| `SlowConsumers` | `int?` | Slow consumer count when available. |
| `MemoryBytes` | `long?` | Memory usage. |
| `StorageBytes` | `long?` | Storage usage when exposed. |
| `InMsgsPerSecond` | `double?` | Derived from cumulative counters. |
| `OutMsgsPerSecond` | `double?` | Derived from cumulative counters. |
| `InBytesPerSecond` | `double?` | Derived from cumulative counters. |
| `OutBytesPerSecond` | `double?` | Derived from cumulative counters. |
| `LastObservedAt` | `DateTimeOffset` | Last successful observation. |
| `MetricStates` | `MetricState[]` | Per-metric `Live`, `Derived`, `Stale`, or `Unavailable` labels. |

**Validation rules**:
- Counter-derived rates must use consecutive usable snapshots and never use unavailable snapshots as baselines.
- Unknown metric fields remain nullable and must render as unavailable in the UI.
- A server disappearing between observations is retained as `Stale` for the troubleshooting window.

---

## 3. `TopologyRelationship`

A safe relationship between topology nodes.

| Field | Type | Description |
|-------|------|-------------|
| `EnvironmentId` | `Guid` | Environment scope. |
| `RelationshipId` | `string` | Stable deterministic id from source/target/type. |
| `SourceNodeId` | `string` | Source server/topology node id. |
| `TargetNodeId` | `string` | Target server/topology node id. |
| `Type` | `TopologyRelationshipType` | `Route`, `Gateway`, `LeafNode`, or `ClusterPeer`. |
| `Direction` | `RelationshipDirection` | `Inbound`, `Outbound`, `Bidirectional`, or `Unknown`. |
| `Status` | `RelationshipStatus` | `Healthy`, `Warning`, `Stale`, `Unavailable`, or `Unknown`. |
| `Freshness` | `ObservationFreshness` | Relationship freshness. |
| `ObservedAt` | `DateTimeOffset` | Observation timestamp. |
| `SourceEndpoint` | `MonitoringEndpoint` | `/routez`, `/gatewayz`, `/leafz`, or compatible source. |
| `SafeLabel` | `string` | Display label with sensitive details omitted or masked. |

**Invariants**:
- Relationships from different environments are not merged.
- Account/operator JWT content is excluded.
- External gateway/leaf relationships are clearly labeled as external when applicable.

---

## 4. `ClusterTopologyGraph`

Frontend projection for React Flow.

```typescript
export interface ClusterTopologyGraph {
  environmentId: string;
  observedAt: string;
  freshness: 'Live' | 'Stale' | 'Partial' | 'Unavailable';
  nodes: ClusterTopologyNode[];
  edges: ClusterTopologyEdge[];
  omittedCounts: {
    filteredNodes: number;
    filteredEdges: number;
    unsafeRelationships: number;
  };
}

export interface ClusterTopologyNode {
  id: string;
  type: 'server' | 'routePeer' | 'gateway' | 'leafnode' | 'external';
  label: string;
  status: 'Healthy' | 'Warning' | 'Stale' | 'Unavailable' | 'Unknown';
  serverId?: string;
  metadata: Record<string, string | number | boolean | null>;
}

export interface ClusterTopologyEdge {
  id: string;
  source: string;
  target: string;
  relationshipType: 'Route' | 'Gateway' | 'LeafNode' | 'ClusterPeer';
  direction: 'Inbound' | 'Outbound' | 'Bidirectional' | 'Unknown';
  status: 'Healthy' | 'Warning' | 'Stale' | 'Unavailable' | 'Unknown';
  freshness: 'Live' | 'Stale' | 'Partial' | 'Unavailable';
}
```

---

## 5. State Transitions

```text
Unavailable ──successful observation──▶ Live
Live ──partial endpoint failure───────▶ Partial
Live/Partial ──no refresh in threshold▶ Stale
Stale ──successful observation────────▶ Live
Any ──environment disabled/unreachable▶ Unavailable
```

---

## 6. Enumerations

| Enum | Values |
|------|--------|
| `ClusterStatus` | `Healthy`, `Degraded`, `Unavailable`, `Unknown` |
| `ServerStatus` | `Healthy`, `Warning`, `Stale`, `Unavailable`, `Unknown` |
| `RelationshipStatus` | `Healthy`, `Warning`, `Stale`, `Unavailable`, `Unknown` |
| `ObservationFreshness` | `Live`, `Stale`, `Partial`, `Unavailable` |
| `MetricState` | `Live`, `Derived`, `Stale`, `Unavailable` |
| `TopologyRelationshipType` | `Route`, `Gateway`, `LeafNode`, `ClusterPeer` |
| `RelationshipDirection` | `Inbound`, `Outbound`, `Bidirectional`, `Unknown` |
| `MonitoringEndpoint` | `Healthz`, `Varz`, `Jsz`, `Routez`, `Gatewayz`, `Leafz`, `Other` |


# API Contracts: Cluster Observability

**Branch**: `copilot/cluster-observability`  
**Date**: 2026-04-28

---

## 1. REST Endpoints

All endpoints require an authenticated session and preserve selected environment context. Responses must not include payload content, account JWTs, or operator JWTs.

### 1.1 Get Cluster Overview

```http
GET /api/environments/{environmentId:guid}/monitoring/cluster/overview
```

Returns the latest cluster-level observation and server summary.

**Response `200 OK`**:

```json
{
  "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "observedAt": "2026-04-28T12:00:00Z",
  "status": "Degraded",
  "freshness": "Partial",
  "serverCount": 3,
  "degradedServerCount": 1,
  "jetStreamAvailable": true,
  "connectionCount": 420,
  "inMsgsPerSecond": 1250.5,
  "outMsgsPerSecond": 1248.2,
  "warnings": [
    {
      "code": "StaleServer",
      "severity": "Warning",
      "message": "server-b has not refreshed within the configured freshness window",
      "serverId": "server-b"
    }
  ],
  "servers": [
    {
      "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "serverId": "server-a",
      "serverName": "nats-a",
      "clusterName": "prod",
      "version": "2.10.18",
      "uptimeSeconds": 86400,
      "status": "Healthy",
      "freshness": "Live",
      "connections": 140,
      "maxConnections": 65536,
      "slowConsumers": 0,
      "memoryBytes": 125829120,
      "storageBytes": 536870912,
      "inMsgsPerSecond": 410.1,
      "outMsgsPerSecond": 409.9,
      "inBytesPerSecond": 1048576.0,
      "outBytesPerSecond": 1024000.0,
      "lastObservedAt": "2026-04-28T12:00:00Z"
    }
  ]
}
```

**Response `400 Bad Request`**: Monitoring not configured for the environment.  
**Response `404 Not Found`**: Environment not found.  
**Response `503 Service Unavailable`**: No usable monitoring data exists and all endpoint attempts failed.

---

### 1.2 Get Cluster Topology

```http
GET /api/environments/{environmentId:guid}/monitoring/cluster/topology?types=Route,Gateway,LeafNode&status=Warning&includeStale=true
```

Returns a bounded graph projection suitable for React Flow.

**Query parameters**:

| Name | Type | Description |
|------|------|-------------|
| `types` | comma-separated enum | Optional relationship types: `Route`, `Gateway`, `LeafNode`, `ClusterPeer`. |
| `status` | enum | Optional relationship/node status filter. |
| `includeStale` | boolean | Include stale nodes/edges. Defaults to `true`. |
| `maxNodes` | integer | Bounded graph size. Defaults to 250; maximum 1000. |

**Response `200 OK`**:

```json
{
  "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "observedAt": "2026-04-28T12:00:00Z",
  "freshness": "Partial",
  "nodes": [
    {
      "id": "server-a",
      "type": "server",
      "label": "nats-a",
      "status": "Healthy",
      "serverId": "server-a",
      "metadata": {
        "version": "2.10.18",
        "clusterName": "prod"
      }
    },
    {
      "id": "gateway-eu",
      "type": "gateway",
      "label": "gateway: eu",
      "status": "Warning",
      "metadata": {
        "external": true
      }
    }
  ],
  "edges": [
    {
      "id": "server-a__Gateway__gateway-eu",
      "source": "server-a",
      "target": "gateway-eu",
      "relationshipType": "Gateway",
      "direction": "Bidirectional",
      "status": "Warning",
      "freshness": "Live",
      "sourceEndpoint": "gatewayz"
    }
  ],
  "omittedCounts": {
    "filteredNodes": 4,
    "filteredEdges": 9,
    "unsafeRelationships": 0
  }
}
```

---

## 2. Frontend Contract

### `useClusterObservability(environmentId: string | null)`

```typescript
interface UseClusterObservabilityResult {
  overview: ClusterObservation | null;
  topology: ClusterTopologyGraph | null;
  isLoading: boolean;
  isRefreshing: boolean;
  error: string | null;
  refetch: () => Promise<void>;
}
```

**Behavior**:
1. Does not fetch until `environmentId` is non-null.
2. Fetches overview and topology through TanStack Query.
3. Preserves stale data during refresh and updates freshness labels.
4. Converts topology response to `@xyflow/react` nodes/edges in the view layer only.

---

## 3. Error Semantics

| Status | Meaning | UI Behavior |
|--------|---------|-------------|
| `400` | Monitoring URL not configured | Show setup guidance. |
| `404` | Environment not found | Show environment error and navigation recovery. |
| `422` | Invalid topology filters | Show validation message next to filters. |
| `503` | Monitoring endpoints unavailable | Show unavailable state with retry. |


# API Contracts: Resource Relationship Map

**Branch**: `copilot/resource-relationship-map`  
**Date**: 2026-04-28

---

## 1. REST Endpoints

All endpoints require an authenticated session. All responses are scoped to a single environment and exclude payload content, credentials, account JWTs, and operator JWTs.

### 1.1 Get Relationship Map

```http
GET /api/environments/{environmentId:guid}/relationships/map?resourceType=Stream&resourceId=orders&depth=1&includeInferred=true&includeStale=true&maxNodes=100&maxEdges=500
```

Returns a bounded focal-resource relationship graph.

**Query parameters**:

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `resourceType` | enum | Yes | Focal resource type. |
| `resourceId` | string | Yes | Focal resource id in selected environment. |
| `depth` | integer | No | 1–3 hops. Defaults to 1. |
| `resourceTypes` | comma-separated enum | No | Include only matching node types. |
| `relationshipTypes` | comma-separated enum | No | Include only matching edge types. |
| `healthStates` | comma-separated enum | No | Include only matching health states. |
| `minimumConfidence` | enum | No | `High`, `Medium`, `Low`, or `Unknown`. Defaults to `Low`. |
| `includeInferred` | boolean | No | Defaults to `true`. |
| `includeStale` | boolean | No | Defaults to `true`. |
| `maxNodes` | integer | No | 1–500. Defaults to 100. |
| `maxEdges` | integer | No | 1–2000. Defaults to 500. |

**Response `200 OK`**:

```json
{
  "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "generatedAt": "2026-04-28T12:00:00Z",
  "depth": 1,
  "focalResource": {
    "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "resourceType": "Stream",
    "resourceId": "orders",
    "displayName": "orders",
    "route": "/environments/3fa85f64-5717-4562-b3fc-2c963f66afa6/jetstream/streams/orders"
  },
  "nodes": [
    {
      "nodeId": "stream:orders",
      "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "resourceType": "Stream",
      "resourceId": "orders",
      "displayName": "orders",
      "status": "Healthy",
      "freshness": "Live",
      "isFocal": true,
      "detailRoute": "/environments/3fa85f64-5717-4562-b3fc-2c963f66afa6/jetstream/streams/orders",
      "metadata": {
        "subjects": "orders.*"
      }
    },
    {
      "nodeId": "consumer:orders:billing",
      "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "resourceType": "Consumer",
      "resourceId": "orders/billing",
      "displayName": "billing",
      "status": "Warning",
      "freshness": "Live",
      "isFocal": false,
      "detailRoute": "/environments/3fa85f64-5717-4562-b3fc-2c963f66afa6/jetstream/streams/orders/consumers/billing",
      "metadata": {
        "ackPending": 42
      }
    }
  ],
  "edges": [
    {
      "edgeId": "stream:orders__Contains__consumer:orders:billing",
      "environmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "sourceNodeId": "stream:orders",
      "targetNodeId": "consumer:orders:billing",
      "relationshipType": "Contains",
      "direction": "Outbound",
      "observationKind": "Observed",
      "confidence": "High",
      "freshness": "Live",
      "status": "Warning",
      "evidence": [
        {
          "sourceModule": "JetStream",
          "evidenceType": "ConsumerParent",
          "observedAt": "2026-04-28T12:00:00Z",
          "freshness": "Live",
          "summary": "Consumer billing belongs to stream orders",
          "safeFields": {
            "stream": "orders",
            "consumer": "billing"
          }
        }
      ]
    }
  ],
  "filters": {
    "depth": 1,
    "includeInferred": true,
    "includeStale": true,
    "maxNodes": 100,
    "maxEdges": 500
  },
  "omittedCounts": {
    "filteredNodes": 0,
    "filteredEdges": 0,
    "collapsedNodes": 12,
    "collapsedEdges": 18,
    "unsafeRelationships": 0
  }
}
```

**Response `400 Bad Request`**: Missing focal resource parameters.  
**Response `404 Not Found`**: Focal resource not found in the selected environment.  
**Response `422 Unprocessable Entity`**: Invalid filter values or bounds.  
**Response `503 Service Unavailable`**: Required source modules are unavailable and no partial map can be generated.

---

### 1.2 Resolve Relationship Node

```http
GET /api/environments/{environmentId:guid}/relationships/nodes/{nodeId}
```

Returns navigation and current status metadata for a selected graph node.

**Response `200 OK`**:

```json
{
  "nodeId": "consumer:orders:billing",
  "resourceType": "Consumer",
  "resourceId": "orders/billing",
  "displayName": "billing",
  "status": "Warning",
  "freshness": "Live",
  "detailRoute": "/environments/3fa85f64-5717-4562-b3fc-2c963f66afa6/jetstream/streams/orders/consumers/billing",
  "canRecenter": true
}
```

---

## 2. Frontend Contract

### `useResourceRelationshipMap(input)`

```typescript
interface UseResourceRelationshipMapInput {
  environmentId: string | null;
  resourceType: ResourceType | null;
  resourceId: string | null;
  filters: MapFilter;
}

interface UseResourceRelationshipMapResult {
  map: RelationshipMap | null;
  isLoading: boolean;
  isFetching: boolean;
  error: string | null;
  recenter: (node: ResourceNode) => void;
  openDetails: (node: ResourceNode) => void;
}
```

**Behavior**:
1. Does not fetch until environment id, resource type, and resource id are available.
2. Uses TanStack Query with filter values in the query key.
3. Converts `RelationshipMap.nodes` and `RelationshipMap.edges` to `@xyflow/react` node/edge view models.
4. Preserves previous graph during refresh and updates stale/freshness labels.

---

## 3. Error Semantics

| Status | Meaning | UI Behavior |
|--------|---------|-------------|
| `400` | Missing focal resource | Show map setup error. |
| `404` | Focal resource missing/deleted | Show missing-resource state with navigation recovery. |
| `422` | Invalid filters | Show inline filter validation. |
| `503` | Source modules unavailable | Show partial/unavailable state with retry. |


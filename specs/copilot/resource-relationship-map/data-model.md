# Data Model: Resource Relationship Map

**Branch**: `copilot/resource-relationship-map`  
**Date**: 2026-04-28

---

## 1. `FocalResource`

The selected resource around which the relationship map is centered.

| Field | Type | Description |
|-------|------|-------------|
| `EnvironmentId` | `Guid` | Selected environment scope. |
| `ResourceType` | `ResourceType` | Type of focal resource. |
| `ResourceId` | `string` | Stable resource identifier within the environment. |
| `DisplayName` | `string` | User-facing label. |
| `Route` | `string?` | Existing resource-detail route for navigation. |

**Validation rules**:
- `EnvironmentId`, `ResourceType`, and `ResourceId` are required.
- Focal resource must resolve through an existing module query or return `404`.
- Focal resource cannot point to a different environment.

---

## 2. `ResourceNode`

A visible node in the relationship graph.

| Field | Type | Description |
|-------|------|-------------|
| `NodeId` | `string` | Deterministic id (`environment:type:id`). |
| `EnvironmentId` | `Guid` | Node environment scope. |
| `ResourceType` | `ResourceType` | Stream, consumer, subject, service, bucket, server, alert, event, etc. |
| `ResourceId` | `string` | Stable resource identifier. |
| `DisplayName` | `string` | Label shown in the map. |
| `Status` | `ResourceHealthStatus` | `Healthy`, `Warning`, `Degraded`, `Stale`, `Unavailable`, or `Unknown`. |
| `Freshness` | `RelationshipFreshness` | Freshness of supporting data. |
| `IsFocal` | `bool` | True for the focal resource. |
| `DetailRoute` | `string?` | Existing detail page route when available. |
| `Metadata` | `SafeMetadata` | Small safe key/value metadata. |

**Invariants**:
- Metadata excludes payload content, credentials, account JWTs, and operator JWTs.
- Cross-environment nodes are omitted unless represented as clearly labeled safe external references.

---

## 3. `RelationshipEdge`

A relationship between two resource nodes.

| Field | Type | Description |
|-------|------|-------------|
| `EdgeId` | `string` | Deterministic id from source/target/type/evidence source. |
| `EnvironmentId` | `Guid` | Environment scope. |
| `SourceNodeId` | `string` | Source node id. |
| `TargetNodeId` | `string` | Target node id. |
| `RelationshipType` | `RelationshipType` | Semantic relationship type. |
| `Direction` | `RelationshipDirection` | `Inbound`, `Outbound`, `Bidirectional`, or `Unknown`. |
| `ObservationKind` | `ObservationKind` | `Observed` or `Inferred`. |
| `Confidence` | `RelationshipConfidence` | `High`, `Medium`, `Low`, or `Unknown`. |
| `Freshness` | `RelationshipFreshness` | `Live`, `Stale`, `Partial`, or `Unavailable`. |
| `Status` | `ResourceHealthStatus` | Health of the relationship/evidence. |
| `Evidence` | `RelationshipEvidence[]` | Safe explanation of why the edge exists. |

**Validation rules**:
- Source and target nodes must exist in the returned graph.
- Inferred edges must include evidence and confidence.
- Direction is `Unknown` when not safely known; do not infer direction from payload content.

---

## 4. `RelationshipEvidence`

Safe metadata explaining an edge.

| Field | Type | Description |
|-------|------|-------------|
| `SourceModule` | `RelationshipSourceModule` | Module that supplied evidence. |
| `EvidenceType` | `string` | Examples: `StreamSubject`, `ConsumerParent`, `ServiceEndpointSubject`, `AlertAffectedResource`. |
| `ObservedAt` | `DateTimeOffset?` | Observation time when available. |
| `Freshness` | `RelationshipFreshness` | Evidence freshness. |
| `Summary` | `string` | Human-readable explanation. |
| `SafeFields` | `SafeMetadata` | Non-sensitive supporting fields. |

---

## 5. `RelationshipMap`

Response root returned to the frontend.

| Field | Type | Description |
|-------|------|-------------|
| `EnvironmentId` | `Guid` | Selected environment. |
| `FocalResource` | `FocalResource` | Center resource. |
| `GeneratedAt` | `DateTimeOffset` | UTC generation time. |
| `Depth` | `int` | Returned traversal depth. |
| `Nodes` | `ResourceNode[]` | Visible nodes. |
| `Edges` | `RelationshipEdge[]` | Visible relationships. |
| `Filters` | `MapFilter` | Applied filters. |
| `OmittedCounts` | `OmittedCounts` | Counts for filtered/collapsed/unsafe data. |

---

## 6. `MapFilter`

User-selected criteria controlling visible graph content.

| Field | Type | Default |
|-------|------|---------|
| `Depth` | `int` | `1` |
| `ResourceTypes` | `ResourceType[]?` | `null` = all supported |
| `RelationshipTypes` | `RelationshipType[]?` | `null` = all supported |
| `HealthStates` | `ResourceHealthStatus[]?` | `null` = all |
| `MinimumConfidence` | `RelationshipConfidence?` | `Low` |
| `IncludeInferred` | `bool` | `true` |
| `IncludeStale` | `bool` | `true` |
| `MaxNodes` | `int` | `100` |
| `MaxEdges` | `int` | `500` |

**Validation rules**:
- `Depth` range: 1–3.
- `MaxNodes` range: 1–500.
- `MaxEdges` range: 1–2000.

---

## 7. Enumerations

| Enum | Values |
|------|--------|
| `ResourceType` | `Server`, `Subject`, `Stream`, `Consumer`, `KvBucket`, `KvKey`, `ObjectBucket`, `Object`, `Service`, `Endpoint`, `Alert`, `Event`, `External` |
| `RelationshipType` | `Contains`, `ConsumesFrom`, `PublishesTo`, `SubscribesTo`, `UsesSubject`, `BackedByStream`, `HostedOn`, `AffectedBy`, `RelatedEvent`, `DependsOn`, `ExternalReference` |
| `RelationshipDirection` | `Inbound`, `Outbound`, `Bidirectional`, `Unknown` |
| `ObservationKind` | `Observed`, `Inferred` |
| `RelationshipConfidence` | `High`, `Medium`, `Low`, `Unknown` |
| `RelationshipFreshness` | `Live`, `Stale`, `Partial`, `Unavailable` |
| `ResourceHealthStatus` | `Healthy`, `Warning`, `Degraded`, `Stale`, `Unavailable`, `Unknown` |
| `RelationshipSourceModule` | `CoreNats`, `JetStream`, `KeyValue`, `ObjectStore`, `Services`, `Monitoring`, `Alerts`, `Events`, `Search` |

---

## 8. State Transitions

```text
Focal selected ──resource resolves──▶ Map ready
Map ready ──node recentered────────▶ New focal resource, same environment
Map ready ──filters changed────────▶ Recomputed bounded graph
Map ready ──evidence stale─────────▶ Stale/partial labels retained
Any ──resource deleted/missing─────▶ Missing focal or stale node state
Any ──unsafe evidence required─────▶ Relationship omitted and counted
```


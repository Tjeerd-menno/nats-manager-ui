# Research: Resource Relationship Map

**Branch**: `copilot/resource-relationship-map`  
**Date**: 2026-04-28  
**Purpose**: Consolidate implementation decisions for focal-resource dependency maps.

---

## 1. Relationship Read-Model Strategy

**Decision**: Create a `Relationships` read-model/query module that composes existing resource modules rather than owning source resources.

**Rationale**: Relationship maps cut across Core NATS, JetStream, KV, Object Store, Services, Monitoring, Search, Alerts, and Events. A composition module keeps traversal/filter/evidence behavior consistent while preserving existing module ownership.

**Alternatives considered**:
- **Put graph logic in every resource module**: Rejected due to duplicated graph construction and inconsistent confidence/freshness rules.
- **Create persisted global graph database**: Rejected as overkill and contrary to safe ephemeral metadata assumptions.

---

## 2. Relationship Sources

**Decision**: Derive relationship edges from safe metadata already visible elsewhere in the application.

**Rationale**: The feature should explain operational dependencies without inspecting payloads or unsafe account/operator internals.

| Source Area | Relationship Examples |
|-------------|-----------------------|
| JetStream | Stream covers subjects; consumer belongs to stream; consumer delivery/filter subjects |
| Core NATS | Subject hierarchy and observed subscription/service usage where safely available |
| Services | Service endpoint uses subject; service group/version relationships |
| KV Store | KV bucket backed by JetStream stream; key belongs to bucket |
| Object Store | Object bucket backed by JetStream stream; object belongs to bucket |
| Monitoring | Server hosts/observes resources; stale or degraded server relationships |
| Alerts/Events | Alert/event affects a resource node |

**Alternatives considered**:
- **Payload inspection to infer producers/consumers**: Rejected because payload reveal is out of scope and unsafe by default.
- **Operator JWT parsing for cross-account relationships**: Rejected as future work unless safe metadata is exposed.

---

## 3. Observed vs Inferred Relationships

**Decision**: Every relationship edge includes `observed`/`inferred`, confidence, freshness, and evidence.

**Rationale**: Some relationships are directly configured (consumer belongs to stream), while others are inferred from subject overlap or service metadata. Operators need to understand certainty before acting during incidents.

**Alternatives considered**:
- **Show all edges as equal**: Rejected because it can mislead operators.
- **Hide inferred edges**: Rejected because inferred subject/service relationships are useful when clearly labeled.

---

## 4. React Flow Visualization

**Decision**: Use React Flow via `@xyflow/react` for dependency maps, added during implementation as a new frontend dependency.

**Rationale**: React Flow supports interactive graph rendering, panning, zooming, node selection, custom node/edge components, keyboard interactions, and deterministic data-driven rendering. The current package name is `@xyflow/react`; legacy `reactflow` should not be introduced.

**Planning constraint**: Do not edit `package.json` in this planning task.

**Alternatives considered**:
- **Mantine-only cards/lists**: Useful as fallback but not sufficient for graph visualization.
- **Custom canvas/SVG**: Rejected due to interaction, accessibility, and testing complexity.

---

## 5. Bounded Neighborhoods

**Decision**: Return a bounded neighborhood around the focal resource by default, with depth/type/status/confidence filters.

**Rationale**: Environments can contain thousands of subjects or objects. A focused graph keeps maps understandable and prevents UI performance regressions.

**Defaults**:
- `depth`: 1 hop
- `maxNodes`: 100
- `maxEdges`: 500
- Include unhealthy/stale direct neighbors even when lower-confidence branches are collapsed.

**Alternatives considered**:
- **Always show all relationships**: Rejected due to visual overload and performance risk.
- **Only direct edges forever**: Rejected because incident traversal needs controlled expansion.

---

## 6. Alerts and Events Integration

**Decision**: Represent alerts/events as annotations or nodes linked to affected resources when safe identifiers are available.

**Rationale**: This turns isolated warnings into impact analysis without requiring users to manually correlate resource detail pages.

**Alternatives considered**:
- **Separate alerts panel only**: Rejected because the spec requires active alerts/events to link to affected resource nodes.
- **External notification integration**: Rejected as future work; alerts remain in-app first.

---

## 7. Security and Privacy

**Decision**: Exclude payload content, credentials, account JWTs, operator JWTs, and unsafe cross-environment data from graph evidence.

**Rationale**: Relationship mapping should operate on safe operational metadata. If a relationship cannot be represented safely, the API returns omitted counts and explanatory evidence rather than exposing sensitive data.

**Alternatives considered**:
- **Store graph evidence history**: Rejected because current troubleshooting does not require retention and persistence increases sensitivity.


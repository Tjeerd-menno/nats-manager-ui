# Research: Cluster Observability

**Branch**: `copilot/cluster-observability`  
**Date**: 2026-04-28  
**Purpose**: Consolidate implementation decisions for cluster health summaries, server comparison, and safe topology visualization.

---

## 1. Module Placement

**Decision**: Extend the existing `Monitoring` bounded context instead of introducing a separate cluster module.

**Rationale**: Cluster observability consumes live operational monitoring data, shares freshness/unavailable semantics, and should appear under the same navigation area as live environment monitoring. This avoids duplicating polling, caching, authorization, and environment-context logic.

**Alternatives considered**:
- **New `Cluster` bounded context**: Rejected because the feature is read-only operational telemetry rather than cluster administration.
- **Dashboard-only widgets**: Rejected because sortable server comparison and topology inspection need dedicated interactions.

---

## 2. NATS Monitoring Endpoint Coverage

**Decision**: Build cluster observations from safe NATS HTTP monitoring/discovery endpoints: `/varz`, `/jsz`, `/healthz`, `/routez`, `/gatewayz`, and `/leafz`, with optional compatible metadata when available.

**Rationale**: These endpoints expose read-only server, JetStream, and topology data without requiring payload inspection. `/routez`, `/gatewayz`, and `/leafz` provide the required relationship coverage for routes, gateways, and leafnodes.

| Endpoint | Used For | Safety Notes |
|----------|----------|--------------|
| `/healthz` | Reachability and health status | Store status/freshness only |
| `/varz` | Server identity, version, uptime, connection pressure, memory, message/byte counters | Safe operational metadata only |
| `/jsz` | JetStream availability and aggregate counts | No message payloads |
| `/routez` | Route peers and route health | Omit unsafe account/operator details |
| `/gatewayz` | Gateway links and remote gateway status | External relationships clearly labeled |
| `/leafz` | Leafnode links and status | Account/operator details omitted unless exposed as safe metadata |

**Alternatives considered**:
- **System account requests**: Rejected for this feature because they require additional permissions and operator/account policy decisions.
- **Browser calls directly to monitoring ports**: Rejected; backend must mediate requests to keep credentials/network topology server-side.

---

## 3. Observation Freshness and Health Derivation

**Decision**: Represent each metric and relationship with explicit freshness (`Live`, `Stale`, `Partial`, `Unavailable`) and source status (`Observed`, `Derived`, `Unavailable`).

**Rationale**: NATS monitoring data may be absent, partial, or temporarily stale. Operators need confidence labels instead of silently missing or misleading topology.

**Alternatives considered**:
- **Single boolean healthy/unhealthy state**: Rejected because it hides partial topology and stale server data.
- **Omitting stale servers immediately**: Rejected because disappearing servers are important incident signals.

---

## 4. Topology Visualization with React Flow

**Decision**: Use React Flow via `@xyflow/react` for topology diagrams, added as a new frontend dependency during implementation.

**Rationale**: React Flow provides React-native graph primitives, panning/zooming, node/edge selection, minimap/controls support, and accessibility hooks suitable for route/gateway/leaf topology views. The package name for current React Flow releases is `@xyflow/react`.

**Implementation note**: This planning task must not modify `package.json`; implementation tasks will add and pin `@xyflow/react`.

**Alternatives considered**:
- **Legacy `reactflow` package**: Rejected because the maintained package is `@xyflow/react`.
- **Custom SVG/canvas**: Rejected due to duplicated interaction, accessibility, and testing complexity.
- **Static table only**: Rejected because it does not satisfy topology visualization acceptance scenarios.

---

## 5. Scaling Strategy

**Decision**: Render a bounded topology by default and provide filters for relationship type, status, and freshness before layout.

**Rationale**: Large clusters can create dense route/gateway/leaf graphs. Filtering before render keeps the UI responsive and supports the constitution performance targets.

**Alternatives considered**:
- **Render the full graph always**: Rejected because hundreds of servers and links can overwhelm operators and the browser.
- **Server-side image rendering**: Rejected because it removes interactive selection and filtering.

---

## 6. Data Retention and Security

**Decision**: Keep cluster observations ephemeral and avoid persisting payloads, JWT content, or sensitive operator/account details.

**Rationale**: The feature is operational observability. Persisting snapshots or sensitive topology internals would increase data handling risk without satisfying a current requirement.

**Alternatives considered**:
- **SQLite historical topology tables**: Rejected because the feature requires current troubleshooting context, not long-term analytics.
- **Full account/operator JWT graphing**: Rejected as future work unless safe metadata is directly exposed.


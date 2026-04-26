# Feature Specification: Cluster Observability

**Feature Branch**: `copilot/cluster-observability`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for cluster observability, using existing SpecKit conventions and default assumptions for NATS 2.x, safe metadata, ephemeral payload handling, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand cluster health at a glance (Priority: P1)

An operator opens a selected environment and sees a cluster-wide observability view that summarizes server health, route health, leafnode/gateway presence when exposed, JetStream availability, current connection pressure, and whether observations are fresh or stale.

**Why this priority**: Operators need immediate confidence about whether the NATS environment is healthy before drilling into specific resources.

**Independent Test**: Can be tested by selecting an environment with monitoring data and confirming the cluster overview shows health, freshness, and degraded indicators without requiring any other feature.

**Acceptance Scenarios**:

1. **Given** a selected environment with reachable monitoring information, **When** the operator opens Cluster Observability, **Then** a summary indicates overall status, server count, leader/peer signals when available, connection count, message rates, and last observed time.
2. **Given** one server reports degraded or stale data, **When** the overview renders, **Then** the overall cluster status reflects the degraded condition and identifies the affected server.
3. **Given** monitoring data is unavailable, **When** the page loads, **Then** the operator sees a clear unavailable state and guidance to verify monitoring configuration.

---

### User Story 2 - Compare server-level metrics (Priority: P2)

An operator compares servers in a cluster to identify imbalance, unusually high connections, slow consumers, high memory, storage pressure, or message rate anomalies.

**Why this priority**: Cluster incidents are often caused by one overloaded or disconnected node; comparison reduces time to isolate the problem.

**Independent Test**: Can be tested by viewing a cluster with multiple servers and verifying sortable/filterable server rows expose comparable status and metric values.

**Acceptance Scenarios**:

1. **Given** multiple servers are observed, **When** the operator sorts by connection count, **Then** servers are ordered by current connection load.
2. **Given** a server has a high slow-consumer count, **When** the server list renders, **Then** that server is visually marked as requiring attention.
3. **Given** metrics differ significantly between servers, **When** the operator expands a server row, **Then** the view shows recent trend context for the selected server.

---

### User Story 3 - Inspect topology signals safely (Priority: P3)

An operator inspects discovered topology relationships such as routes, gateways, leafnodes, and cluster peers using only safe metadata exposed by supported NATS monitoring/discovery sources.

**Why this priority**: Topology context helps explain outages while respecting the plan boundary that deeper multi-account/operator JWT support is future work.

**Independent Test**: Can be tested by connecting to an environment that exposes topology metadata and confirming the view identifies known relationships without revealing sensitive account details.

**Acceptance Scenarios**:

1. **Given** route metadata is available, **When** topology signals are displayed, **Then** route peers are listed with status and last observation time.
2. **Given** account/operator JWT details are not safely exposed, **When** the topology view renders, **Then** those details are omitted and no misleading placeholder is shown.
3. **Given** topology metadata changes, **When** the next observation refresh occurs, **Then** added or missing relationships are reflected in the view.

---

### Edge Cases

- What happens when only a single NATS server is observed? The feature still shows server health and clearly labels topology comparison as not available for a single-node environment.
- What happens when servers report incompatible or partial metric fields? Missing fields are labeled unavailable; the remaining metrics continue to render.
- What happens when a server disappears between observations? The server is retained as stale for the current troubleshooting window with a last-seen timestamp.
- How does the system handle environments with hundreds of observed servers? Server lists must remain searchable and filterable, and aggregate health must remain visible without scrolling.
- What happens when monitoring data exposes identifiers that may be sensitive? The UI shows only safe operational metadata and masks or omits account/operator-level details.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-CLU-001**: System MUST provide a cluster observability view scoped to the currently selected environment.
- **FR-CLU-002**: System MUST show overall cluster status using available server health, freshness, and degradation signals.
- **FR-CLU-003**: System MUST list observed servers with name or identifier, status, version, uptime, connection count, message rates, byte rates, memory indicators, and last observed time when available.
- **FR-CLU-004**: System MUST indicate whether each displayed metric is live, stale, derived, or unavailable.
- **FR-CLU-005**: System MUST highlight servers with operational warning signals such as stale observations, high slow-consumer counts, high connection pressure, storage pressure, or unavailable health.
- **FR-CLU-006**: System MUST allow users to sort, search, and filter server observations by status, name/identifier, version, and notable warning state.
- **FR-CLU-007**: System MUST present topology relationships exposed through safe monitoring/discovery metadata, including routes, gateways, leafnodes, and cluster peers when available.
- **FR-CLU-008**: System MUST treat multi-account and operator JWT inspection as future work unless safe metadata is directly exposed by monitoring/discovery sources.
- **FR-CLU-009**: System MUST preserve environment context on every cluster observability screen and prevent observations from different environments from being combined.
- **FR-CLU-010**: System MUST provide a degraded-state experience when monitoring/discovery data is unavailable, partial, or stale.
- **FR-CLU-011**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ behavior for displayed metrics.
- **FR-CLU-012**: System MUST avoid persisting payload content or sensitive operational details as part of cluster observability.

### Key Entities

- **Cluster Observation**: A point-in-time summary of cluster-level health, freshness, server count, and aggregate warning state for one environment.
- **Server Observation**: Metrics and health signals for an observed NATS server, including identity, status, uptime, version, resource pressure, and last observed time.
- **Topology Relationship**: A safe, discovered relationship between servers or NATS topology elements such as routes, gateways, leafnodes, and peers.
- **Observation Freshness**: The state that explains whether displayed data is current, stale, partial, or unavailable.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-CLU-001**: Operators can identify whether a selected cluster is healthy, degraded, or unavailable within 10 seconds of opening the view.
- **SC-CLU-002**: Operators can identify the most concerning server in a degraded cluster within 30 seconds using visual status and sorting.
- **SC-CLU-003**: 95% of cluster overview refreshes show updated freshness indicators within one configured monitoring interval.
- **SC-CLU-004**: Server lists remain usable for at least 250 observed servers, with search/filter interactions completing within 1 second.
- **SC-CLU-005**: No account/operator JWT content or message payload content is displayed unless exposed as safe non-sensitive metadata.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, with behavior targeted at NATS 2.10+.
- Cluster observability depends on monitoring/discovery data already available for the selected environment.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload inspection is not part of this feature and any observed payload-related data is ephemeral and not persisted.
- Production environments disable invasive live tapping/replay/payload reveal by default unless an administrator explicitly permits it.
- Alerts derived from cluster health are in-app first; external notifications are future work.

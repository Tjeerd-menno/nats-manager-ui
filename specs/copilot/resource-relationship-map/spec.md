# Feature Specification: Resource Relationship Map

**Feature Branch**: `copilot/resource-relationship-map`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for a resource relationship map, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visualize relationships around a resource (Priority: P1)

An operator selects a stream, consumer, subject, service, KV bucket, object bucket, or server and sees a relationship map showing connected resources, observed directionality, health, and confidence/freshness.

**Why this priority**: Troubleshooting often requires understanding what depends on what; a focused map around one resource is immediately useful and bounded.

**Independent Test**: Can be tested by selecting a resource with known relationships and verifying the map shows connected nodes and links with environment context.

**Acceptance Scenarios**:

1. **Given** a stream with consumers and subjects, **When** the user opens its relationship map, **Then** the map shows the stream, related consumers, subject coverage, and health state.
2. **Given** a service endpoint uses a subject, **When** relationship metadata is available, **Then** the map links the endpoint to the subject with a freshness indicator.
3. **Given** relationship data is partial, **When** the map renders, **Then** partial relationships are labeled with confidence/freshness rather than presented as complete.

---

### User Story 2 - Traverse dependencies during an incident (Priority: P2)

An operator follows relationship links from an alert or unhealthy resource to adjacent streams, consumers, subjects, services, and storage resources to identify likely impact.

**Why this priority**: Traversal turns isolated alerts into operational impact analysis.

**Independent Test**: Can be tested by starting from an unhealthy consumer and navigating to its stream, covered subjects, and related service/producer metadata when available.

**Acceptance Scenarios**:

1. **Given** an alert references a consumer, **When** the operator opens the relationship map, **Then** the affected consumer is highlighted and connected to its stream.
2. **Given** adjacent resources have warning states, **When** the map renders, **Then** those warning states are visible without opening each resource.
3. **Given** the user selects a connected node, **When** they navigate, **Then** the map recenters on that node while preserving environment context.

---

### User Story 3 - Filter and simplify large maps (Priority: P3)

An operator filters the map by resource type, health state, relationship confidence, and distance from the focal resource to avoid overload in large environments.

**Why this priority**: Relationship maps can become noisy at scale; filtering is necessary after the core map and traversal flows are valuable.

**Independent Test**: Can be tested by applying type and depth filters in a large environment and verifying the map remains comprehensible and responsive.

**Acceptance Scenarios**:

1. **Given** a map contains more than 100 related nodes, **When** the user limits depth to one hop, **Then** only direct relationships are shown.
2. **Given** the user filters to unhealthy resources, **When** the filter is applied, **Then** healthy-only branches are hidden or de-emphasized.
3. **Given** no relationships match a filter, **When** the map updates, **Then** a clear empty-filter state is displayed.

---

### Edge Cases

- What happens when relationships are inferred rather than directly observed? The map labels them as inferred and shows lower confidence.
- What happens when a resource has thousands of relationships? The map starts with a bounded neighborhood and requires filtering or expansion to show more.
- What happens when resources are renamed or deleted? Stale nodes are marked missing or last-seen until refreshed.
- What happens when cross-account/operator relationships would require JWT inspection? Those relationships are omitted unless safe metadata is exposed.
- What happens when map data spans multiple environments? The feature prevents cross-environment mixing unless explicitly represented as a safe external relationship.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-RRM-001**: System MUST provide a resource relationship map scoped to the selected environment and focal resource.
- **FR-RRM-002**: System MUST represent supported resource types including servers, subjects, streams, consumers, KV buckets/keys, Object Store buckets/objects, services, endpoints, alerts, and events when relationships are available.
- **FR-RRM-003**: System MUST show relationship type, directionality when known, health state, confidence, and freshness for each link.
- **FR-RRM-004**: System MUST distinguish observed relationships from inferred relationships.
- **FR-RRM-005**: System MUST allow users to navigate from a node to that resource's detail view or recenter the map on that node.
- **FR-RRM-006**: System MUST allow filtering by resource type, health state, relationship type, confidence, and distance from focal resource.
- **FR-RRM-007**: System MUST show bounded neighborhoods by default to keep large maps usable.
- **FR-RRM-008**: System MUST omit multi-account/operator JWT relationship details unless safe metadata is exposed by monitoring/discovery endpoints.
- **FR-RRM-009**: System MUST prevent relationships from different environments from being mixed in one map unless represented as clearly labeled external references.
- **FR-RRM-010**: System MUST avoid showing or persisting payload content as part of relationship mapping.
- **FR-RRM-011**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ metadata behavior.
- **FR-RRM-012**: System MUST link active alerts/events to affected resource nodes when safe identifiers are available.

### Key Entities

- **Resource Node**: A visible resource in the map, such as stream, consumer, subject, service, bucket, server, alert, or event.
- **Relationship Edge**: A connection between resource nodes with type, directionality, confidence, freshness, and source of observation.
- **Focal Resource**: The selected resource around which the map is centered.
- **Map Filter**: User-selected criteria controlling which nodes and relationships are visible.
- **Relationship Evidence**: Safe metadata explaining why a relationship is shown.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-RRM-001**: Users can open a relationship map from a supported resource detail page in 2 clicks or fewer.
- **SC-RRM-002**: Users can identify direct unhealthy neighboring resources within 30 seconds.
- **SC-RRM-003**: Maps with up to 500 available relationships render an initial bounded neighborhood in under 2 seconds.
- **SC-RRM-004**: 95% of displayed relationships include either observed/inferred status or freshness/confidence labels.
- **SC-RRM-005**: Cross-environment or unsafe account/operator relationships are never displayed without clear safe metadata.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- Relationship data is derived from safe metadata already visible elsewhere in the application.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload inspection is not needed for relationship mapping and must not be persisted.
- Production environments disable live tapping, replay, and payload reveal by default unless an administrator explicitly permits it.
- Alerts are in-app first and may appear as map nodes or resource annotations; external notifications are future work.

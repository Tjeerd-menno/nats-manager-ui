# Feature Specification: Services Debug Console

**Feature Branch**: `copilot/services-debug-console`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for a Services debug console, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover and inspect services (Priority: P1)

A developer or operator opens the Services debug console and sees discovered NATS services with identity, version, endpoint names, health, metadata labels, and last discovery time.

**Why this priority**: Service discovery is the safest and most valuable first step for understanding what service endpoints exist before sending any request.

**Independent Test**: Can be tested by connecting to an environment with NATS services and confirming discovered service and endpoint metadata appears with freshness indicators.

**Acceptance Scenarios**:

1. **Given** services are discoverable in the selected environment, **When** the console opens, **Then** the user sees service names, versions, endpoint groups, endpoint names, health, and last discovery time when available.
2. **Given** a service exposes descriptive metadata, **When** the user opens service detail, **Then** metadata is labeled as auto-discovered and not treated as authoritative documentation.
3. **Given** no services are discovered, **When** the console loads, **Then** an empty state explains that no services were discovered.

---

### User Story 2 - Send controlled service test requests (Priority: P2)

An authorized user sends a test request to a selected service endpoint with an explicit warning that the request may have side effects, then inspects the response metadata and optional payload.

**Why this priority**: Test requests are a high-value debugging capability but can affect application behavior, so they require discovery and safeguards first.

**Independent Test**: Can be tested in a non-production environment by selecting a known test endpoint, sending a bounded request, and verifying the response outcome appears.

**Acceptance Scenarios**:

1. **Given** an authorized user selects an endpoint, **When** they prepare a request, **Then** the console shows environment, service, endpoint, payload size, timeout, and side-effect warning.
2. **Given** the user confirms the request, **When** the service responds, **Then** response status, latency, headers/metadata when available, and masked payload preview are shown.
3. **Given** production service requests are not permitted, **When** the user attempts a test request, **Then** the action is disabled or blocked with an explanation.

---

### User Story 3 - Compare endpoint health and latency (Priority: P3)

An operator compares endpoint health and response latency observations to identify degraded services or endpoints that need application-team attention.

**Why this priority**: Health and latency trends help diagnose service issues beyond a single request but depend on accurate discovery and safe request controls.

**Independent Test**: Can be tested by viewing service endpoints with varying health states and confirming the console highlights degraded endpoints and recent latency.

**Acceptance Scenarios**:

1. **Given** a service has multiple endpoints, **When** the service detail renders, **Then** each endpoint shows health status and last observed latency when available.
2. **Given** an endpoint becomes unhealthy, **When** discovery or health data refreshes, **Then** the endpoint is marked degraded and the related service summary reflects it.
3. **Given** health data is stale, **When** the user reviews the endpoint, **Then** the stale state is visible and not presented as current health.

---

### Edge Cases

- What happens when service discovery returns partial or malformed metadata? The console shows safe available fields and labels missing fields as unavailable.
- What happens when a service test request times out? The response panel shows timeout, elapsed time, and no payload.
- What happens when a response payload is large or binary? Payload is masked by default, bounded by size limits, and not persisted.
- What happens when two services share a display name? The console distinguishes them using safe identity and environment metadata.
- What happens when endpoint invocation could trigger business side effects? The console requires explicit confirmation and obeys production safeguards.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-SVC-001**: System MUST provide a Services debug console scoped to the selected environment.
- **FR-SVC-002**: System MUST discover and list services with safe metadata including name, identity, version, endpoint groups, endpoint names, health, and last discovery time when available.
- **FR-SVC-003**: System MUST label discovered metadata as auto-discovered and not authoritative business documentation.
- **FR-SVC-004**: System MUST allow users to search and filter services by name, endpoint, health, version, and metadata label.
- **FR-SVC-005**: System MUST allow authorized users to send bounded test requests to service endpoints only after explicit confirmation.
- **FR-SVC-006**: System MUST warn that service test requests may have side effects before invocation.
- **FR-SVC-007**: System MUST show test request outcomes including status, elapsed time, error summary, response metadata, and masked payload preview when available.
- **FR-SVC-008**: System MUST keep request and response payload inspection ephemeral and avoid persistence unless a future specification explicitly requires it.
- **FR-SVC-009**: System MUST disable service replay and payload reveal by default in production unless an administrator explicitly permits it.
- **FR-SVC-010**: System MUST record user-initiated service test requests as auditable operational events.
- **FR-SVC-011**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ service discovery behavior.
- **FR-SVC-012**: System MUST surface degraded service health as in-app alerts/events where alerting is enabled.

### Key Entities

- **Discovered Service**: A NATS service discovered in an environment, including safe identity, version, metadata labels, and health state.
- **Service Endpoint**: A callable endpoint belonging to a discovered service, with subject, group, health, and observed latency when available.
- **Service Test Request**: A user-confirmed diagnostic invocation with request metadata, limits, and outcome.
- **Service Response Preview**: Ephemeral response metadata and optionally masked payload content for an active diagnostic session.
- **Service Health Observation**: A point-in-time health or latency observation for a service or endpoint.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-SVC-001**: Users can find a discovered service or endpoint by name within 30 seconds in environments with up to 500 endpoints.
- **SC-SVC-002**: 100% of service test requests require explicit confirmation that includes environment, service, endpoint, and side-effect warning.
- **SC-SVC-003**: 95% of service test request outcomes display status and elapsed time within 2 seconds after completion or timeout.
- **SC-SVC-004**: No request or response payload content remains available after the diagnostic session ends.
- **SC-SVC-005**: Degraded service health is visible in the service list and related in-app event views within one discovery/observation interval.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- Service discovery metadata is auto-discovered and may be incomplete; it is not authoritative business documentation.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload inspection for service requests and responses is ephemeral and must not be persisted unless a future spec explicitly requires it.
- Production environments disable live tapping, replay, and payload reveal by default unless an administrator explicitly permits it.
- Alerts are in-app first; external notifications are future work.

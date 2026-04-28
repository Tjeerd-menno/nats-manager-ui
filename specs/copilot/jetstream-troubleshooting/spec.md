# Feature Specification: JetStream Troubleshooting

**Feature Branch**: `copilot/jetstream-troubleshooting`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for JetStream troubleshooting, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Identify unhealthy streams and consumers (Priority: P1)

An operator opens JetStream Troubleshooting and sees a prioritized list of streams and consumers with health indicators for backlog growth, delivery stalls, acknowledgment problems, storage pressure, and retention risk.

**Why this priority**: Stalled consumers and storage pressure are common production incidents; surfacing them first delivers immediate operational value.

**Independent Test**: Can be tested by connecting to an environment with streams/consumers and confirming the view identifies healthy, warning, and critical resources independently of mutation features.

**Acceptance Scenarios**:

1. **Given** JetStream is enabled with active streams and consumers, **When** the operator opens the troubleshooting view, **Then** streams and consumers are grouped by severity and issue type.
2. **Given** a consumer backlog is growing, **When** the view refreshes, **Then** that consumer is marked with backlog trend and recommended next inspection steps.
3. **Given** JetStream is disabled, **When** the view opens, **Then** the user sees an informative disabled-state message.

---

### User Story 2 - Diagnose a single stream or consumer (Priority: P2)

An operator selects a problematic stream or consumer and sees configuration, state, recent observations, advisories, and related resources needed to understand the issue.

**Why this priority**: After a problem is detected, users need enough context to decide whether to inspect messages, adjust configuration, or involve an application team.

**Independent Test**: Can be tested by selecting a known consumer and verifying delivery state, pending messages, ack details, redelivery indicators, and related stream context are visible.

**Acceptance Scenarios**:

1. **Given** a selected consumer, **When** the detail view opens, **Then** it shows delivery state, pending messages, redelivery count, acknowledgment policy, durable name, and last observed time when available.
2. **Given** a selected stream, **When** the detail view opens, **Then** it shows configured limits, storage usage, message count, subject coverage, replicas, and retention policy.
3. **Given** a detail value is unavailable, **When** the page renders, **Then** the missing value is labeled unavailable rather than guessed.

---

### User Story 3 - Inspect messages safely for troubleshooting (Priority: P3)

An authorized user inspects message metadata and optionally reveals payload content in a bounded, ephemeral session to diagnose poison messages or unexpected data.

**Why this priority**: Message inspection is useful but can expose sensitive content; it should follow detection and diagnosis flows with explicit safeguards.

**Independent Test**: Can be tested in a non-production environment by selecting a stream message, viewing metadata, revealing payload content, and confirming no payload persists after the session.

**Acceptance Scenarios**:

1. **Given** a stream has messages, **When** the user opens message inspection, **Then** message sequence, timestamp, headers metadata, size, and delivery context are shown when available.
2. **Given** payload reveal is permitted, **When** the user explicitly reveals content, **Then** the payload is displayed only for the active session and marked as potentially sensitive.
3. **Given** production payload reveal is not administratively permitted, **When** the user attempts reveal, **Then** the action is blocked with an explanation.

---

### Edge Cases

- What happens when a stream has millions of messages? The feature shows summary, sampling, and bounded inspection rather than attempting to load all messages.
- What happens when consumers are deleted while the view is open? The resource is marked missing and the user is prompted to refresh related data.
- What happens when message payloads are encrypted, binary, or compressed? The UI shows metadata, size, and type hints without corrupting the payload display.
- What happens when server observations disagree across cluster peers? The view labels the state as inconsistent and shows last observation time per source when available.
- What happens when a troubleshooting action could affect delivery state? The action is treated as high impact and requires explicit authorization and confirmation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-JST-001**: System MUST provide a JetStream troubleshooting view scoped to the selected environment.
- **FR-JST-002**: System MUST identify and prioritize stream and consumer issues including backlog growth, stalled delivery, redelivery spikes, storage pressure, retention risk, and unavailable state.
- **FR-JST-003**: System MUST clearly distinguish healthy, warning, critical, unavailable, and unknown resource states.
- **FR-JST-004**: System MUST show stream detail context including configuration, state, storage usage, subject coverage, retention policy, replicas, and last observed time when available.
- **FR-JST-005**: System MUST show consumer detail context including delivery state, pending messages, redelivery indicators, acknowledgment settings, durable identity, and last observed time when available.
- **FR-JST-006**: System MUST provide issue explanations and suggested next inspection steps without performing corrective actions automatically.
- **FR-JST-007**: System MUST allow authorized message metadata inspection with bounded result counts.
- **FR-JST-008**: System MUST mask payload content by default and keep payload inspection ephemeral with no persistence unless a future spec explicitly requires it.
- **FR-JST-009**: System MUST disable replay, payload reveal, and other live high-impact troubleshooting actions by default in production unless an administrator explicitly permits them.
- **FR-JST-010**: System MUST label inconsistent, stale, or partial JetStream observations and preserve last-known usable data.
- **FR-JST-011**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ JetStream behavior.
- **FR-JST-012**: System MUST emit in-app operational events for critical JetStream findings; external notifications are future work.

### Key Entities

- **JetStream Issue**: A detected condition affecting a stream or consumer, including severity, evidence, affected resource, and recommended next step.
- **Stream Diagnostic View**: Troubleshooting context for a stream, combining configuration, state, limits, usage, and related consumers.
- **Consumer Diagnostic View**: Troubleshooting context for a consumer, combining backlog, delivery, acknowledgment, redelivery, and health signals.
- **Message Inspection Session**: A bounded, ephemeral session for viewing message metadata and optionally payload content.
- **Troubleshooting Advisory**: A user-facing explanation of a likely problem and safe next action.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-JST-001**: Operators can identify the highest-severity JetStream issue in an environment within 30 seconds.
- **SC-JST-002**: 95% of stream and consumer health observations display a freshness timestamp.
- **SC-JST-003**: Users can navigate from an issue summary to the relevant stream or consumer detail in 2 clicks or fewer.
- **SC-JST-004**: Message inspection sessions respect configured count/size limits in 100% of validation runs.
- **SC-JST-005**: No payload content inspected for troubleshooting remains available after the inspection session ends.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- JetStream resources are visible only when JetStream is enabled for the selected environment.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload inspection is ephemeral and must not be persisted unless a future spec explicitly requires it.
- Production environments disable live tapping, replay, and payload reveal by default unless an administrator explicitly permits it.
- Alerts are in-app first; external notifications are future work.

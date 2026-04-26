# Feature Specification: Core NATS Debug Console

**Feature Branch**: `copilot/core-nats-debug-console`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for a Core NATS debug console, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inspect subjects and subscriptions (Priority: P1)

An operator opens the Core NATS debug console for a selected environment and explores observed subjects, subscriptions, connected clients, and traffic characteristics without publishing or tapping live payloads.

**Why this priority**: Read-only inspection delivers immediate troubleshooting value with the lowest operational risk.

**Independent Test**: Can be tested by opening the console in an environment with active subscriptions and confirming subject and subscription metadata appears with freshness and environment context.

**Acceptance Scenarios**:

1. **Given** a connected environment with active subscriptions, **When** the operator opens the console, **Then** the view lists known subjects or subject patterns, subscription counts, and last observed activity when available.
2. **Given** a subject is selected, **When** its detail view opens, **Then** the operator sees related subscriptions, client identifiers when safe, and traffic indicators.
3. **Given** no subject data is available, **When** the console loads, **Then** an empty state explains what data is missing and how to proceed safely.

---

### User Story 2 - Publish controlled diagnostic messages (Priority: P2)

An authorized user sends a diagnostic message to a subject after reviewing the selected environment, subject, payload preview, and potential impact warning.

**Why this priority**: Controlled publish is a common debugging action, but it must be gated because it can affect live systems.

**Independent Test**: Can be tested by sending a diagnostic message to a known test subject and confirming the console reports success or failure and records the action.

**Acceptance Scenarios**:

1. **Given** an authorized user enters a subject and payload, **When** they initiate publish, **Then** the console shows a confirmation with environment, subject, payload size, and impact warning.
2. **Given** the user confirms, **When** the publish completes, **Then** the console displays the outcome and timestamp.
3. **Given** the selected environment is classified as production and live diagnostic actions are not permitted, **When** the user attempts to publish, **Then** the action is disabled or blocked with an explanation.

---

### User Story 3 - Temporarily observe live traffic (Priority: P3)

An authorized user temporarily subscribes to a subject pattern to observe live messages for troubleshooting, with explicit time limits, payload reveal controls, and no payload persistence.

**Why this priority**: Live observation is powerful but high risk; it should be available only after safe read-only and controlled publish flows exist.

**Independent Test**: Can be tested in a non-production environment by enabling a temporary tap on a test subject and confirming messages appear only during the active session.

**Acceptance Scenarios**:

1. **Given** live tapping is permitted for the environment, **When** the user starts a temporary subscription, **Then** the console shows captured message metadata and masks payload content until explicitly revealed.
2. **Given** the tap duration limit is reached, **When** time expires, **Then** the console stops receiving messages and clearly marks the session ended.
3. **Given** the user leaves the console, **When** the session ends, **Then** no captured payload content is retained.

---

### Edge Cases

- What happens when a wildcard subject pattern is too broad? The console warns about impact and requires narrowing or explicit elevated permission.
- What happens when payload content is binary or too large? Payload content is masked by default, size/type metadata is shown, and reveal/download follows explicit limits.
- What happens when a publish succeeds but no subscriber receives it? The console reports publish completion without implying delivery unless delivery evidence is available.
- How does the system handle connection loss during a live tap? The tap stops, the session is marked interrupted, and no automatic replay is attempted.
- What happens when a user tries to run live actions in production? Live tapping, replay, and payload reveal are disabled by default unless an administrator explicitly permits them.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-CND-001**: System MUST provide a Core NATS debug console scoped to the currently selected environment.
- **FR-CND-002**: System MUST allow users to inspect observed subject, subscription, client, and traffic metadata when safely available.
- **FR-CND-003**: System MUST distinguish observed metadata from inferred or unavailable data.
- **FR-CND-004**: System MUST allow authorized users to publish diagnostic messages only after explicit confirmation.
- **FR-CND-005**: System MUST show environment, subject, payload size, and impact warning before any publish action is confirmed.
- **FR-CND-006**: System MUST support temporary live subscription/tap sessions only when permitted by environment policy and user authorization.
- **FR-CND-007**: System MUST mask message payloads by default and require explicit user action before revealing content.
- **FR-CND-008**: System MUST ensure payload inspection is ephemeral and payload content is not persisted unless a future specification explicitly requires it.
- **FR-CND-009**: System MUST enforce time, count, and size limits for live observation sessions.
- **FR-CND-010**: System MUST disable live tapping, replay, and payload reveal by default in production environments unless an administrator explicitly permits them.
- **FR-CND-011**: System MUST record user-initiated publish and live-observation actions as auditable operational events with actor, environment, subject, timestamp, and outcome.
- **FR-CND-012**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ behavior.

### Key Entities

- **Debug Console Session**: A user-scoped troubleshooting session for one selected environment, including active mode, start/end time, and authorization state.
- **Subject Observation**: Safe metadata about a subject or subject pattern, including subscription count, recent activity, and freshness.
- **Diagnostic Publish Request**: A user-confirmed request to publish a message to a subject for troubleshooting.
- **Live Tap Session**: A temporary, bounded subscription used to observe message metadata and optionally reveal payloads during an active session.
- **Payload Preview**: Ephemeral content view with type, size, masking, truncation, and reveal state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-CND-001**: Operators can locate a known subject or subscription pattern within 30 seconds in an active environment.
- **SC-CND-002**: 100% of publish actions require explicit confirmation showing environment and subject before execution.
- **SC-CND-003**: Live tap sessions automatically stop at or before the configured time/count limit in 100% of test runs.
- **SC-CND-004**: No captured payload content remains available after the live tap session ends or the user leaves the console.
- **SC-CND-005**: In production-classified environments, live tap/replay/payload reveal controls are disabled by default.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- Subject and subscription visibility depends on safe metadata exposed by the selected environment.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload inspection is ephemeral and must not be persisted unless a future spec explicitly requires it.
- Production environments disable live tapping, replay, and payload reveal by default unless an administrator explicitly permits it.
- Alerts or warnings generated by the console are in-app first; external notifications are future work.

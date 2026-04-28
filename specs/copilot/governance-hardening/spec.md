# Feature Specification: Governance Hardening

**Feature Branch**: `copilot/governance-hardening`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for governance hardening, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enforce environment safety policies (Priority: P1)

An administrator classifies environments and configures policy defaults so production environments block or require explicit permission for live tapping, replay, payload reveal, destructive actions, and high-impact debug operations.

**Why this priority**: Governance must prevent risky debugging actions from being available by default in production.

**Independent Test**: Can be tested by classifying an environment as production and confirming restricted controls are hidden, disabled, or blocked across debug features.

**Acceptance Scenarios**:

1. **Given** an environment is classified as production, **When** a user opens a debug console, **Then** live tapping, replay, and payload reveal are disabled by default.
2. **Given** an administrator explicitly permits a restricted capability, **When** an authorized user opens the relevant feature, **Then** the capability becomes available with warnings and audit/event recording.
3. **Given** an unauthorized user attempts a restricted action, **When** the action is requested, **Then** it is blocked with a clear explanation.

---

### User Story 2 - Apply consistent sensitive-data safeguards (Priority: P2)

An administrator defines masking, reveal, truncation, and session behavior for payloads, KV values, object content, service request/response bodies, and connection secrets.

**Why this priority**: Sensitive data handling must be consistent across all inspection and debug features before broad production usage.

**Independent Test**: Can be tested by opening each content-bearing feature and confirming content is masked by default, reveals require authorization, and content disappears after the session.

**Acceptance Scenarios**:

1. **Given** a user views payload-capable content, **When** the content panel opens, **Then** sensitive content is masked by default with size/type metadata visible.
2. **Given** a user reveals content, **When** the session ends, **Then** revealed content is no longer available.
3. **Given** a value exceeds configured limits, **When** the user inspects it, **Then** the UI shows truncation or size-limit messaging.

---

### User Story 3 - Review governance events and policy outcomes (Priority: P3)

An auditor reviews policy decisions, blocked actions, explicit administrator permits, content reveal events, destructive actions, and high-impact debug operations.

**Why this priority**: Governance is only effective when policy decisions and sensitive actions are traceable.

**Independent Test**: Can be tested by attempting allowed and blocked actions, then filtering governance events by user, environment, policy, action type, and outcome.

**Acceptance Scenarios**:

1. **Given** a restricted action is blocked, **When** an auditor opens governance events, **Then** the event includes actor, environment, action, policy reason, timestamp, and outcome.
2. **Given** an administrator changes a policy, **When** the event history is reviewed, **Then** the change is visible with actor, previous/new policy summary, and timestamp.
3. **Given** a user reveals payload content, **When** governance events are filtered by reveal action, **Then** the event appears without storing the revealed payload.

---

### Edge Cases

- What happens when an environment classification is missing? The system treats it as non-production only if explicitly configured; otherwise restricted actions remain conservative.
- What happens when policy changes while a live debug session is active? The session is re-evaluated and restricted capabilities stop if no longer permitted.
- What happens when an audit/governance event cannot be recorded? The high-impact action is blocked or marked failed-safe.
- What happens when users have overlapping roles? The most restrictive applicable production safety rule wins unless an explicit administrator permit applies.
- What happens when sensitive content is revealed accidentally? The event records reveal metadata but not content, and the session can be terminated immediately.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-GOV-001**: System MUST allow administrators to classify environments by operational sensitivity, including production.
- **FR-GOV-002**: System MUST disable live tapping, replay, payload reveal, and high-impact debug actions by default for production environments unless an administrator explicitly permits them.
- **FR-GOV-003**: System MUST provide policy controls for content masking, reveal authorization, truncation, session lifetime, and size/count limits.
- **FR-GOV-004**: System MUST apply sensitive-content safeguards consistently across Core NATS, JetStream, Services, KV, and Object Store debug features.
- **FR-GOV-005**: System MUST prevent payload/value/object/request/response content from being persisted unless a future specification explicitly requires it.
- **FR-GOV-006**: System MUST require explicit confirmation for destructive or high-impact actions and show environment, target, action, and risk summary.
- **FR-GOV-007**: System MUST record governance events for policy changes, blocked actions, permitted restricted actions, content reveals, destructive actions, and high-impact debug operations.
- **FR-GOV-008**: System MUST ensure governance events contain actor, environment, action, target metadata, policy reason, timestamp, and outcome without storing revealed content.
- **FR-GOV-009**: System MUST allow authorized users to search and filter governance events by environment, actor, action type, policy, outcome, and time range.
- **FR-GOV-010**: System MUST fail safe by blocking high-impact actions when policy or governance event recording cannot be evaluated reliably.
- **FR-GOV-011**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ operational behavior.
- **FR-GOV-012**: System MUST treat multi-account/operator JWT inspection as future work unless safe metadata is exposed by monitoring/discovery endpoints.

### Key Entities

- **Environment Classification**: The sensitivity label and policy context for an environment, such as production or non-production.
- **Governance Policy**: A set of rules controlling restricted actions, content reveal, destructive operations, limits, and confirmation requirements.
- **Restricted Capability Permit**: An administrator-approved exception enabling a normally disabled capability for a defined environment/scope.
- **Governance Event**: A traceable record of policy decisions, restricted actions, content reveals, policy changes, and outcomes.
- **Sensitive Content Control**: The masking, reveal, truncation, and lifetime rules applied to payloads, values, objects, requests, and responses.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-GOV-001**: 100% of production-classified environments disable live tapping, replay, and payload reveal by default.
- **SC-GOV-002**: 100% of high-impact actions require confirmation showing environment, target, action, and risk summary.
- **SC-GOV-003**: 100% of content reveal events record governance metadata without storing revealed content.
- **SC-GOV-004**: Authorized auditors can locate a governance event by actor and time range within 30 seconds.
- **SC-GOV-005**: Policy changes take effect for new sessions immediately and for active sessions within one policy re-evaluation interval.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- Production environments disable live tapping, replay, and payload reveal by default unless an administrator explicitly permits them.
- Payload inspection is ephemeral and must not be persisted unless a future spec explicitly requires it.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Alerts are in-app first; external notifications are future work.
- Governance hardening applies to existing and planned debug/observability features consistently rather than creating separate operational workflows.

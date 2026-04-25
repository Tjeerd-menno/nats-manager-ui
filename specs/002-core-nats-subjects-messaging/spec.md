# Feature Specification: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Feature Branch**: `002-core-nats-subjects-messaging`  
**Created**: 2026-04-25  
**Status**: Draft  
**Input**: User description: "Rework the Core NATS section to show the subjects. Greatly expand the publishing function and allow viewing of messages"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Active Subjects (Priority: P1)

An operator opens the Core NATS page for a connected environment and immediately sees a list of all subjects that currently have active subscribers. Each entry shows the subject name and the number of active subscriptions. The list auto-refreshes periodically so that subjects that appear or disappear as clients connect are reflected without requiring a manual page reload. The operator can search or filter the subject list by name to quickly locate a specific topic.

**Why this priority**: Knowing which subjects are active is the most fundamental visibility need for Core NATS. Without it, operators have no sense of what is happening on the bus.

**Independent Test**: Can be fully tested by navigating to Core NATS, verifying the subject list renders with at least one entry when a subscriber is active, and confirming a filter reduces the displayed subjects.

**Acceptance Scenarios**:

1. **Given** an environment with active subscriptions, **When** the operator opens the Core NATS page, **Then** a table or list of subjects is displayed, each row showing the subject name and subscriber count.
2. **Given** the subject list is visible, **When** the operator types a filter string, **Then** only subject names matching the filter are shown.
3. **Given** the subject list is visible, **When** 15 seconds pass, **Then** the list silently refreshes and any new or removed subjects are reflected.
4. **Given** a NATS environment where the monitoring endpoint is unavailable, **When** the operator opens Core NATS, **Then** the subjects section shows an informative message explaining that subject discovery is unavailable for this server configuration, and the rest of the page (server info) still renders.

---

### User Story 2 - Publish Messages with Full Control (Priority: P2)

An operator wants to send test or trigger messages to a NATS subject. The current publish form only accepts a subject and free-text payload. The expanded publisher allows the operator to also specify:
- Payload format (plain text, JSON, or raw bytes expressed as a hex string)
- One or more custom message headers (key–value pairs)
- An optional reply-to subject so that the recipient knows where to send a response

The form validates inputs before submission (e.g., JSON payload must be parseable, header keys must be non-empty) and shows a clear success or error state after each publish attempt. The operator can queue and send multiple messages in sequence without reopening the dialog.

**Why this priority**: The current publish form is too limited for real-world debugging. Expanded publishing is the most-requested operator tool for verifying consumer behavior and triggering workflows.

**Independent Test**: Can be fully tested by opening the publish dialog, entering a subject + JSON payload + one header + a reply-to subject, publishing, and verifying the form shows a success state and the message list (Story 3) updates.

**Acceptance Scenarios**:

1. **Given** the publish dialog is open, **When** the operator selects "JSON" as the payload format and types invalid JSON, **Then** an inline validation error appears and the Publish button is disabled.
2. **Given** the publish dialog is open, **When** the operator adds a header with an empty key, **Then** the form shows a validation error for that header row.
3. **Given** a valid subject, JSON payload, two headers, and a reply-to subject are entered, **When** the operator clicks Publish, **Then** the message is sent, the form shows a success notification, and the fields are ready for the next message (not cleared automatically unless the user dismisses the dialog).
4. **Given** the publish dialog is open with filled fields, **When** the operator clicks Publish twice rapidly, **Then** only one message is sent (button is disabled while the first request is in flight).
5. **Given** the publish fails (e.g., no subscribers, server error), **When** the failure response is received, **Then** the form shows an actionable error message without clearing the user's inputs.

---

### User Story 3 - View Live Messages on a Subject (Priority: P3)

An operator wants to observe messages flowing through a Core NATS subject in real time for debugging and monitoring. The operator enters a subject pattern (including wildcards such as `foo.*` or `foo.>`), clicks "Subscribe", and a message log panel appears showing incoming messages as they arrive. Each message entry displays the subject, timestamp, payload (formatted if JSON), and any headers. The operator can pause/resume the feed, clear the log, and set a cap on how many messages are retained in the view (default 100). Clicking a message row expands it to show the full payload and all headers.

**Why this priority**: Message visibility is essential for diagnosing integration problems but lower priority than the subject list and improved publish as it requires server-side subscription management.

**Independent Test**: Can be fully tested by subscribing to a subject, publishing a test message via the publish panel (Story 2), and confirming the message appears in the live feed with correct subject, payload, and headers.

**Acceptance Scenarios**:

1. **Given** the operator enters `test.>` and clicks Subscribe, **When** a message is published to `test.events`, **Then** the message appears in the feed within 2 seconds showing subject, timestamp, and payload.
2. **Given** the message feed is running, **When** the operator clicks Pause, **Then** new messages no longer appear in the list (though they may buffer server-side), and when Resume is clicked the buffered messages appear.
3. **Given** the message feed contains 100 messages and a new message arrives, **When** the cap of 100 is reached, **Then** the oldest message is removed so the list stays within the cap.
4. **Given** a message with JSON payload arrives, **When** the operator expands the message row, **Then** the payload is displayed as pretty-printed JSON.
5. **Given** the operator is subscribed to a subject, **When** they navigate away from the Core NATS page, **Then** the server-side subscription is closed and no further resources are consumed.
6. **Given** the operator subscribes to an invalid subject pattern (e.g., containing spaces), **Then** an inline error is shown before subscribing.

---

### Edge Cases

- What happens when the NATS server does not expose monitoring endpoints (subject list unavailable)? → Show informational placeholder, do not fail the page load.
- What happens when a message payload is binary and not valid UTF-8? → Display the payload as a hex string and label it as binary.
- What happens if a header value is very large? → Truncate display in the list view; show full value on expand.
- What happens when no subjects match the filter? → Show "No subjects match your filter" empty state.
- What happens if the user subscribes with a wildcard that matches a very high-volume subject (thousands of messages/second)? → The message cap (default 100, user-adjustable up to 500) limits memory growth; the UI remains responsive.
- What happens when the publish operation exceeds the server's max-payload limit? → An error message is shown indicating the payload is too large.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Core NATS page MUST display a subject browser section that lists all subjects with active subscribers, showing subject name and subscription count for each.
- **FR-002**: The subject browser MUST auto-refresh on the same 15-second interval as the server status cards.
- **FR-003**: The subject browser MUST provide a text filter so operators can search subjects by name or pattern.
- **FR-004**: When subject discovery is unavailable for a given server configuration, the system MUST display an informative placeholder instead of an error state.
- **FR-005**: The publish form MUST allow operators to specify a payload format: plain text, JSON, or hex-encoded bytes.
- **FR-006**: The publish form MUST validate JSON payloads client-side before submission and prevent publishing if the JSON is malformed.
- **FR-007**: The publish form MUST allow operators to add, edit, and remove arbitrary message headers (key–value pairs).
- **FR-008**: The publish form MUST allow operators to specify an optional reply-to subject.
- **FR-009**: The publish button MUST be disabled while a publish request is in flight to prevent duplicate submissions.
- **FR-010**: On a successful publish, the system MUST display a success notification without clearing the form fields automatically.
- **FR-011**: On a failed publish, the system MUST display a descriptive error message and preserve all form inputs.
- **FR-012**: The Core NATS page MUST include a message viewer panel where operators can subscribe to a subject or wildcard pattern and observe incoming messages in real time.
- **FR-013**: Each message entry in the viewer MUST display: subject, received timestamp, payload preview, and header count.
- **FR-014**: Expanding a message row in the viewer MUST show the full payload (formatted as pretty-printed JSON when the content is valid JSON) and all headers.
- **FR-015**: The message viewer MUST support Pause and Resume controls to temporarily halt display updates.
- **FR-016**: The message viewer MUST enforce a configurable cap on retained messages (default 100, user-adjustable up to 500) to limit memory usage.
- **FR-017**: When the operator navigates away from the Core NATS page, the system MUST close any active server-side subscriptions.
- **FR-018**: Binary (non-UTF-8) message payloads MUST be displayed as a hex string and labelled as binary.
- **FR-019**: Publish actions MUST continue to be recorded in the audit trail.

### Key Entities

- **Subject**: A NATS subject string (e.g., `orders.created`) with an associated active subscription count as reported by the server monitoring interface.
- **PublishRequest**: The data a user sends when publishing — subject, payload format, payload content, optional reply-to subject, and zero or more headers.
- **MessageHeader**: A key–value pair attached to a NATS message.
- **LiveMessage**: A message received from a real-time subscription — subject, arrival timestamp, raw payload bytes (rendered based on content), and headers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Operators can see all active subjects with subscriber counts within 2 seconds of opening the Core NATS page when subjects are available.
- **SC-002**: The subject filter reduces the visible list to matching subjects within 300 milliseconds of the operator typing.
- **SC-003**: A message published with headers and a reply-to subject appears in the live message viewer within 2 seconds of clicking Publish.
- **SC-004**: The message viewer remains visually responsive (no freezing or significant lag) when receiving 50 messages per second with a cap of 100 messages.
- **SC-005**: 100% of publish actions (including those with headers) are recorded in the audit log.
- **SC-006**: Operators can compose and send a publish request (including headers) in under 60 seconds from opening the publish dialog.

## Assumptions

- Subject discovery relies on the NATS HTTP monitoring API (`/subsz` endpoint); environments that do not expose this endpoint will see an "unavailable" placeholder rather than a failure.
- Real-time message viewing is implemented via a server-sent events (SSE) or WebSocket endpoint on the backend that bridges a NATS core subscription; the exact transport mechanism is an implementation decision.
- The message viewer is limited to Core NATS subjects (not JetStream consumer subscriptions, which are covered by the JetStream page).
- Wildcard subscriptions (`*` and `>`) are supported in the message viewer with the same semantics as NATS core wildcard matching.
- The maximum viewer cap of 500 messages is a UI constraint for performance; it does not affect the underlying NATS subscription.
- The existing operator-level authorization requirement on the publish endpoint is retained; read-only users can view subjects and messages but cannot publish.
- Subject names and header keys/values are treated as UTF-8 strings; no encoding conversion is performed by the UI beyond display rendering.
- The existing 15-second polling for server status is reused for subject list refresh; no separate polling interval configuration is added.
- Mobile layout is out of scope; the feature targets desktop-width viewports consistent with the rest of the admin UI.

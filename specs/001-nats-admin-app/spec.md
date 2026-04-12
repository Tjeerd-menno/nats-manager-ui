# Feature Specification: NATS Admin Application

**Feature Branch**: `001-nats-admin-app`
**Created**: 2026-04-05
**Status**: Draft
**Input**: User description: "Build a NATS Admin application which implements these functional specifications"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Connect to a NATS environment and view its status (Priority: P1)

An operator opens the application, registers a NATS environment by providing connection details, and immediately sees the environment's health status — whether it is reachable, degraded, or unavailable. The currently selected environment is always visible so the operator never loses context about which cluster they are working with.

**Why this priority**: Without a working connection and visible environment context, no other feature in the application is usable. This is the foundational capability that everything else depends on.

**Independent Test**: Can be fully tested by registering a NATS environment and confirming that connection status, environment name, and health indicators are displayed. Delivers immediate operational awareness.

**Acceptance Scenarios**:

1. **Given** the application is open with no environments configured, **When** the user registers a new environment with valid connection details, **Then** the environment appears in the environment list with a "connected" status indicator.
2. **Given** a registered environment, **When** the NATS instance becomes unreachable, **Then** the status indicator changes to "unavailable" and a timestamp shows the last successful interaction.
3. **Given** multiple environments are registered, **When** the user selects an environment, **Then** the application displays the selected environment name prominently and all subsequent views are scoped to that environment.
4. **Given** a registered environment, **When** the user triggers a connection test, **Then** the application reports whether the environment is reachable and usable.
5. **Given** a connected environment, **When** data is displayed, **Then** the application indicates whether the data is live, recently refreshed, or potentially stale.

---

### User Story 2 - Browse and inspect JetStream streams and consumers (Priority: P2)

An operator selects a connected environment and navigates to the JetStream section. They see a list of all streams with summary information (message count, storage usage, retention state). They drill into a specific stream to view its details and associated consumers. For each consumer they can see backlog size, delivery state, and health indicators — enabling them to quickly spot stalled consumers or growing backlogs.

**Why this priority**: JetStream is the most operationally complex NATS capability. Stream and consumer visibility is the highest-value inspection feature and the most common reason operators need a management tool.

**Independent Test**: Can be tested by connecting to a NATS environment that has streams and consumers, browsing the stream list, opening a stream detail view, and verifying consumer state is displayed accurately.

**Acceptance Scenarios**:

1. **Given** a connected environment with JetStream enabled, **When** the user navigates to the streams view, **Then** all available streams are listed with name, message count, storage usage, and retention policy.
2. **Given** the streams list, **When** the user selects a stream, **Then** a detail view shows stream configuration, state, limits, and a list of associated consumers.
3. **Given** a stream detail view, **When** the user selects a consumer, **Then** the consumer's backlog, delivery state, acknowledgment info, and health indicators are displayed.
4. **Given** a stream list with more than 50 streams, **When** the user searches or filters by name, **Then** matching results appear within 1 second.
5. **Given** a consumer with a growing backlog, **When** the user views the consumers list, **Then** the unhealthy consumer is visually highlighted.

---

### User Story 3 - Create, update, and delete JetStream resources with safeguards (Priority: P3)

A platform engineer needs to create a new stream, modify a consumer's configuration, or delete an unused stream. The application clearly distinguishes read-only actions from state-changing actions. Before any destructive operation, the user sees the resource name, environment, and a confirmation prompt. All state-changing actions are recorded for audit.

**Why this priority**: After operators can see what exists (P1, P2), they need to act on it. CRUD operations with proper safeguards are essential for day-to-day administration.

**Independent Test**: Can be tested by creating a stream, modifying its configuration, then deleting it — verifying confirmation prompts and audit records at each step.

**Acceptance Scenarios**:

1. **Given** a connected environment, **When** the user creates a new stream with valid configuration, **Then** the stream appears in the list and a success confirmation is shown.
2. **Given** an existing stream, **When** the user modifies its retention policy, **Then** the update is applied and the detail view reflects the new configuration.
3. **Given** an existing stream, **When** the user initiates deletion, **Then** a confirmation dialog shows the stream name, environment, and impact warning before proceeding.
4. **Given** a destructive action is confirmed, **When** the deletion completes, **Then** the action is recorded in the audit log with user, timestamp, resource, and outcome.
5. **Given** the user has read-only permissions, **When** they view a stream detail page, **Then** state-changing actions are hidden or disabled.

---

### User Story 4 - Inspect and manage Key-Value Store buckets and keys (Priority: P4)

A developer navigates to the KV section, browses available buckets, opens a bucket to see its keys, and inspects the current value and metadata of a specific key. They can distinguish between current, deleted, and superseded key states. When authorized, they can create or update keys with overwrite protection.

**Why this priority**: KV Store is widely used for configuration and coordination. Visibility into keys and their states is critical for debugging distributed systems, but it depends on the foundational connection and navigation patterns established in P1–P3.

**Independent Test**: Can be tested by browsing KV buckets, inspecting key values and revisions, and creating/updating a key with overwrite confirmation.

**Acceptance Scenarios**:

1. **Given** a connected environment with KV buckets, **When** the user navigates to the KV section, **Then** all buckets are listed with name and summary state.
2. **Given** a selected bucket, **When** the user browses its keys, **Then** keys are listed with their current state (current, deleted, superseded).
3. **Given** a specific key, **When** the user inspects it, **Then** the current value, revision history, and relevant metadata are displayed.
4. **Given** a key update, **When** the user changes a value, **Then** the application warns about potential overwrite of coordination data before saving.
5. **Given** a bucket with many keys, **When** the user searches by key name pattern, **Then** matching keys appear within 1 second.

---

### User Story 5 - View environment dashboard with cross-resource health summary (Priority: P5)

An operator opens the application and sees an environment dashboard that summarizes the health of Core NATS connections, JetStream streams and consumers, KV buckets, Object Store buckets, and discovered services. Notable issues (stalled consumers, unavailable services, storage pressure) are surfaced prominently. The operator can drill down from any summary indicator into the detailed resource view.

**Why this priority**: The dashboard provides the "single pane of glass" experience described in the product vision. It depends on individual resource views already functioning (P2–P4) but adds significant value for daily operational monitoring.

**Independent Test**: Can be tested by connecting to an environment with resources across all capability areas and verifying that the dashboard surfaces accurate health summaries with working drill-down links.

**Acceptance Scenarios**:

1. **Given** a connected environment, **When** the user opens the dashboard, **Then** health summaries for Core NATS, JetStream, KV, Object Store, and Services are displayed.
2. **Given** a stalled consumer or unhealthy resource, **When** the dashboard renders, **Then** the issue is visually prominent (severity-based highlighting).
3. **Given** a summary indicator showing a problem, **When** the user clicks on it, **Then** the application navigates to the relevant detail view.
4. **Given** the environment is unreachable, **When** the dashboard loads, **Then** a clear degraded-state message is shown and the UI remains interactive.

---

### User Story 6 - Discover and test NATS Services (Priority: P6)

A developer discovers available services in the environment, views their identity, endpoints, versions, and health status. When authorized, they issue a test request to a service and inspect the response for diagnostic purposes. The application warns before sending requests that may have side effects.

**Why this priority**: Service discovery is valuable for debugging but is a narrower use case than stream/consumer and KV management. It requires the environment connection and navigation patterns to be established first.

**Independent Test**: Can be tested by discovering a service, viewing its metadata, and sending a test request with response inspection.

**Acceptance Scenarios**:

1. **Given** a connected environment with NATS services, **When** the user navigates to the services section, **Then** discovered services are listed with name, version, and status.
2. **Given** a selected service, **When** the user views its detail page, **Then** endpoints, groups, health indicators, and descriptive metadata are displayed.
3. **Given** permission to test, **When** the user sends a test request, **Then** the application shows a side-effect warning and displays the response for diagnostic inspection.
4. **Given** a service is unavailable, **When** the services list renders, **Then** the service is marked as unavailable with appropriate visual distinction.

---

### User Story 7 - Manage Object Store buckets and objects (Priority: P7)

An operator browses Object Store buckets, inspects objects and their metadata, and when authorized uploads, downloads, replaces, or deletes objects. The application warns before actions on large or sensitive objects and indicates when an action may affect downstream systems.

**Why this priority**: Object Store is less commonly used than streams and KV but is part of the unified management experience. It follows the same interaction patterns as KV (P4), making it lower-risk to implement.

**Independent Test**: Can be tested by browsing Object Store buckets, inspecting object metadata, and performing upload/download/delete with confirmation prompts.

**Acceptance Scenarios**:

1. **Given** a connected environment with Object Store buckets, **When** the user navigates to Object Store, **Then** all buckets are listed with name and state.
2. **Given** a selected bucket, **When** the user browses objects, **Then** objects are listed with name, size, and relevant metadata.
3. **Given** permission, **When** the user uploads an object, **Then** the object appears in the bucket and a success message is shown.
4. **Given** a large object, **When** the user initiates download, **Then** the application warns about object size before proceeding.
5. **Given** an object deletion, **When** the user confirms, **Then** the action is recorded in the audit log and the object is removed.

---

### User Story 8 - Inspect Core NATS subjects, clients, and message traffic (Priority: P8)

An operator explores the Core NATS layer: viewing connected clients, active subscriptions, subject hierarchies, and message traffic characteristics. They can publish a test message or subscribe to a subject to observe live traffic. The application warns before actions that may affect live traffic.

**Why this priority**: Core NATS inspection is valuable but most operators interact with JetStream resources more frequently. This story also requires real-time capabilities that build on the foundation of earlier stories.

**Independent Test**: Can be tested by connecting to an environment, browsing subjects and connected clients, and publishing/subscribing to a test subject.

**Acceptance Scenarios**:

1. **Given** a connected environment, **When** the user navigates to Core NATS, **Then** environment status, connected client count, and subject hierarchy are displayed.
2. **Given** the subjects view, **When** the user explores a subject hierarchy, **Then** child subjects, subscription counts, and traffic indicators are shown.
3. **Given** authorization, **When** the user publishes a message to a subject, **Then** a confirmation warns about potential impact on live traffic before sending.
4. **Given** authorization, **When** the user subscribes to a subject, **Then** incoming messages are displayed with metadata and payload content.

---

### User Story 9 - Authenticate, authorize, and audit user actions (Priority: P9)

An administrator configures role-based access so that operators see all resources but cannot perform destructive operations in production, while developers have read-only access. All authentication events, state-changing operations, and authorization-relevant events are recorded in an audit log that can be searched and filtered by user, action, time, and resource.

**Why this priority**: Access control and audit logging are essential for production use but can be implemented after the core inspection and administration features are functional. They wrap around the existing features rather than blocking them.

**Independent Test**: Can be tested by creating users with different roles, verifying permission enforcement across views, and inspecting the audit log for recorded actions.

**Acceptance Scenarios**:

1. **Given** the application, **When** an unauthenticated user attempts access, **Then** they are redirected to authenticate.
2. **Given** a read-only user, **When** they navigate to a stream detail, **Then** all state-changing actions are hidden or disabled.
3. **Given** an administrator, **When** they delete a stream in a production environment, **Then** the audit log records the user, action, resource, environment, timestamp, and outcome.
4. **Given** the audit log, **When** an auditor searches by user and date range, **Then** matching records are returned with complete traceability information.
5. **Given** environment-level restrictions, **When** an operator attempts a destructive action in production, **Then** the action is blocked per the environment policy.

---

### User Story 10 - Search, filter, and navigate across all resource types (Priority: P10)

An operator uses a global search to find resources by name, subject, or identifier across all NATS capability areas. They filter by resource type, environment, and status. They bookmark frequently accessed resources for quick return. Navigation supports progressive disclosure from summaries to detail views.

**Why this priority**: Cross-resource search enhances the experience significantly at scale but depends on all individual resource views being available first. It is an efficiency multiplier rather than a core capability.

**Independent Test**: Can be tested by searching for known resources across different types and verifying results are returned quickly with correct navigation links.

**Acceptance Scenarios**:

1. **Given** resources across multiple capability areas, **When** the user searches by name, **Then** matching resources from all types are returned with type indicators.
2. **Given** search results, **When** the user filters by resource type and status, **Then** results are narrowed accordingly.
3. **Given** a frequently accessed resource, **When** the user bookmarks it, **Then** it is available for quick return from a bookmarks view.
4. **Given** a large environment with 1,000+ resources, **When** the user searches, **Then** results appear within 1 second.

---

### Edge Cases

- What happens when the NATS environment becomes unreachable mid-session? The application must show a degraded-state indicator, display last-known data marked as stale, and remain interactive.
- How does the system handle environments with thousands of streams/consumers? List views must paginate or virtualize to maintain performance, and search/filter must remain responsive.
- What happens when a user attempts a destructive action on a resource that was already deleted? The application must detect the conflict, inform the user, and refresh the view.
- How does the system behave when multiple users modify the same resource simultaneously? The application must communicate whether the action succeeded, failed, or encountered conflicts.
- What happens when connection credentials expire during a session? The application must inform the user and prompt for re-authentication without losing navigation context.
- What happens when a KV key or stream message contains binary or non-displayable content? The application must indicate content type and offer raw view without data corruption.
- How does the system handle NATS environments with JetStream disabled? The application must gracefully hide JetStream, KV, and Object Store sections and show only Core NATS capabilities.

## Requirements *(mandatory)*

### Functional Requirements

**Environment & Connection Management**

- **FR-001**: System MUST allow users to register, view, modify, enable, and disable multiple NATS environment connections.
- **FR-002**: System MUST display the currently selected environment and connection context at all times.
- **FR-003**: System MUST indicate connection status (available, degraded, unavailable) and last successful interaction timestamp.
- **FR-004**: System MUST allow users to test whether an environment is reachable.
- **FR-005**: System MUST clearly indicate when displayed data is live, recently refreshed, or potentially stale.
- **FR-006**: System MUST prevent resources from different environments from being mixed or confused.

**Core NATS**

- **FR-007**: System MUST allow users to inspect environment status, connected clients, and server-level information (version, uptime, max payload). Multi-account and account-level inspection is out of scope for v1 and may be added in a future iteration.
- **FR-008**: System MUST allow users to explore subjects, subject hierarchies, and active subscriptions.
- **FR-009**: System MUST allow users to observe message traffic characteristics at the subject and environment level.
- **FR-010**: System MUST allow authorized users to publish messages to subjects and subscribe to subjects for live message inspection.
- **FR-011**: System MUST warn users before actions that may affect live traffic.

**JetStream**

- **FR-012**: System MUST allow users to view all streams with summary information (message count, storage usage, retention state).
- **FR-013**: System MUST allow users to inspect stream details, state, limits, usage, and associated consumers.
- **FR-014**: System MUST allow users to view consumer state including backlog, delivery state, acknowledgment info, and health indicators.
- **FR-015**: System MUST allow authorized users to create, update, and delete streams and consumers.
- **FR-016**: System MUST allow users to inspect messages stored in streams.
- **FR-017**: System MUST allow users to identify operational issues (stalled consumers, growing backlogs, storage pressure).

**Key-Value Store**

- **FR-018**: System MUST allow users to view all KV buckets, inspect bucket details, and browse keys within a bucket.
- **FR-019**: System MUST allow users to inspect key values, metadata, and revision information.
- **FR-020**: System MUST allow authorized users to create, update, and delete KV buckets and keys.
- **FR-021**: System MUST distinguish between current, deleted, missing, and superseded key states.
- **FR-022**: System MUST warn users before overwriting or deleting coordination-sensitive data.

**Object Store**

- **FR-023**: System MUST allow users to view Object Store buckets, browse objects, and inspect object metadata.
- **FR-024**: System MUST allow authorized users to create, update, and delete buckets and to upload, download, replace, and delete objects.
- **FR-025**: System MUST warn users before actions on large or sensitive objects and indicate downstream impact.

**NATS Services**

- **FR-026**: System MUST allow users to discover services and view identity, endpoints, versions, groups, and health status.
- **FR-027**: System MUST allow authorized users to send test requests to services and inspect responses.
- **FR-028**: System MUST warn before sending requests that may have side effects.
- **FR-029**: System MUST distinguish service discovery information from authoritative business documentation. Service metadata obtained via NATS micro discovery protocol MUST be labeled as "auto-discovered" (e.g., a badge or icon). If no authoritative documentation exists, the system displays only auto-discovered data without implying it is curated.

**Navigation & Search**

- **FR-030**: System MUST provide cross-resource search by name, subject, bucket, stream, consumer, service, or identifier.
- **FR-031**: System MUST allow filtering by resource type, environment, scope, and status.
- **FR-032**: System MUST support hierarchical and grouped navigation with progressive disclosure (summary → detail).
- **FR-033**: System MUST allow users to bookmark frequently accessed resources.

**Message & Payload Inspection**

- **FR-034**: System MUST allow authorized users to inspect messages, keys, objects, and service payloads with clear metadata/payload separation.
- **FR-035**: System MUST support raw content viewing and structured content readability improvements.
- **FR-036**: System MUST clearly indicate when content is truncated, partial, transformed, or unavailable.
- **FR-037**: System MUST provide safeguards for sensitive or confidential content and restrict access by role. Safeguards include: (a) truncating large payloads with a "show full content" opt-in, (b) requiring Operator or higher role to view raw message/key/object payloads, (c) masking credential fields (tokens, passwords, seeds) in environment configuration views for ReadOnly users.

**Administrative Safeguards**

- **FR-038**: System MUST visually distinguish read-only actions from state-changing actions.
- **FR-039**: System MUST require explicit confirmation for destructive or high-impact actions, showing resource name, environment, and impact.
- **FR-040**: System MUST allow actions to be restricted by role, policy, or environment classification.
- **FR-041**: System MUST support a read-only mode where state-changing actions are hidden or disabled.
- **FR-042**: System MUST communicate action outcomes (success, failure, warnings).

**Monitoring & Alerts**

- **FR-043**: System MUST provide environment health dashboards summarizing resource status across all NATS capability areas.
- **FR-044**: System MUST help users detect connectivity issues, resource growth, unhealthy consumers, unavailable services, and unusual activity. Detection is based on observed state comparisons: (a) connection status polling per environment (configurable interval, default 30s), (b) consumer pending message count exceeding 80% of max-deliver threshold, (c) stream storage usage exceeding 80% of configured limit, (d) service health endpoint returning non-OK status, (e) rapid message rate changes (>2x baseline over 5-minute window).
- **FR-045**: System MUST surface severe or urgent conditions more prominently than normal conditions.
- **FR-046**: System MUST present operational events with severity levels and allow users to distinguish informational events from actionable problems.
- **FR-047**: System MUST support notification of important conditions to appropriate users or roles.

**Access Control & Audit**

- **FR-048**: System MUST require user authentication.
- **FR-049**: System MUST support role-based or policy-based authorization with differentiated access for viewing vs. administering resources.
- **FR-050**: System MUST allow organizations to restrict access by environment, resource type, and action type.
- **FR-051**: System MUST record audit events for authentication, state-changing operations, and authorization-relevant events including who, what, when, which resource, and outcome.
- **FR-052**: System MUST allow authorized users to search and filter audit history.
- **FR-053**: System MUST distinguish system-generated events from user-initiated events.

**Usability & Consistency**

- **FR-054**: System MUST use NATS terminology accurately and consistently throughout the interface.
- **FR-055**: System MUST present a consistent management model across all NATS capability areas (streams, consumers, buckets, objects, services).
- **FR-056**: System MUST provide informative error feedback that explains what happened and what the user can do next.
- **FR-057**: System MUST provide contextual guidance for specialized NATS concepts.
- **FR-058**: System MUST indicate whether displayed data is observed, configured, derived, or inferred.

### Key Entities

- **Environment**: A registered NATS deployment (cluster) with connection details, health status, and last-known interaction timestamp. An environment scopes all resource views and actions.
- **Stream**: A JetStream persistence resource with subjects, retention policy, storage limits, message count, and associated consumers.
- **Consumer**: A JetStream subscription resource associated with a stream, with delivery state, backlog, acknowledgment policy, and health indicators.
- **KV Bucket**: A JetStream Key-Value storage container with configuration, key count, and history settings.
- **KV Key**: An entry in a KV Bucket with a name, current value, revision history, and state (current, deleted, superseded).
- **Object Store Bucket**: A JetStream Object Store container with configuration and object count.
- **Object**: A stored binary or structured item in an Object Store Bucket with name, size, and metadata.
- **Service**: A discovered NATS service with identity, version, endpoints, groups, and health status.
- **User**: An authenticated person with a role determining which environments, resources, and actions they can access.
- **Audit Event**: A record of a user or system action including actor, action type, target resource, environment, timestamp, and outcome.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can identify the health status of a NATS environment within 10 seconds of opening the application.
- **SC-002**: Users can locate any specific stream, consumer, bucket, or service by name within 30 seconds using search.
- **SC-003**: Users can navigate from environment dashboard to a specific resource detail view in 3 clicks or fewer.
- **SC-004**: 95% of users correctly identify the current environment context before performing an administrative action (validated through usability testing).
- **SC-005**: Destructive operations require a minimum of 2 deliberate user interactions (initiate + confirm) before execution.
- **SC-006**: Application pages load and become interactive within 2 seconds on a standard connection.
- **SC-007**: Resource list views remain responsive when displaying 1,000+ items (scroll and filter complete within 300ms).
- **SC-008**: All state-changing administrative actions produce a corresponding audit record within 5 seconds.
- **SC-009**: Operators report that troubleshooting common issues (stalled consumers, storage pressure, unreachable environments) is faster than using command-line tools (validated through user feedback).
- **SC-010**: The application provides a consistent interaction pattern across all five NATS capability areas, validated by UX review confirming uniform navigation, list, detail, and action patterns.

## Assumptions

- Users have network access to the NATS environments they need to manage.
- NATS environments expose monitoring endpoints and system subjects required for status, metrics, and resource discovery.
- JetStream, KV Store, and Object Store features are available only when enabled on the target NATS server; the application degrades gracefully when they are disabled.
- Session-based authentication with standard security practices is used unless the organization specifies an alternative.
- The initial release targets desktop browser usage; mobile-optimized layouts are out of scope for v1.
- The application is deployed within the organization's internal network or behind a VPN; public internet exposure is not a design assumption for v1.
- Performance targets assume environments with up to 10,000 total resources (streams + consumers + buckets + keys + objects + services); behavior at higher scale is a follow-up concern.
- Audit log retention follows the organization's standard data retention policy.
- NATS server versions supported will be determined during technical design, but the application should target currently maintained NATS releases.
- .NET Aspire is used for local development orchestration only (AppHost replaces docker-compose); production deployment remains a single OCI container image. The AppHost orchestrates all components: NATS container (with JetStream), ASP.NET Core backend, and Vite frontend dev server. No docker-compose.yml is used; the Dockerfile remains for production builds only.
- The React frontend uses Vite as the build tool and dev server, and Vitest as the frontend test runner. Frontend tests are colocated with source files as `*.test.ts(x)` (idiomatic Vitest convention), not in a separate test project.
- The solution includes a `NatsManager.ServiceDefaults` project for shared OpenTelemetry, health checks, and resilience configuration (standard Aspire pattern), and a `NatsManager.AppHost` project for development orchestration.

## Clarifications

### Session 2026-04-06

- Q: What is Aspire's scope in this project? → A: Dev orchestration only — Aspire AppHost replaces docker-compose for local dev; production still deploys as a single OCI container
- Q: Should the Aspire AppHost also orchestrate the Vite frontend dev server? → A: Full orchestration — AppHost manages NATS container, backend project, AND Vite frontend dev server
- Q: Where should frontend tests live? → A: Colocated — tests live alongside source in `src/NatsManager.Frontend/src/` as `*.test.ts(x)` files (idiomatic Vitest convention)
- Q: Should the solution include a ServiceDefaults project? → A: Yes — add `NatsManager.ServiceDefaults` with shared OpenTelemetry, health checks, and resilience configuration (standard Aspire pattern)
- Q: What happens to docker-compose.yml? → A: Remove entirely — Aspire AppHost fully replaces docker-compose for local development; Dockerfile remains for production builds

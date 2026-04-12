# Functional Specification: NATS Management Application

---

## 1. Purpose

This specification defines the functional requirements for an application used to manage NATS environments. The application is intended to provide operational visibility and administrative control over:

- Core NATS
- JetStream
- Key-Value Store
- Object Store
- NATS Services

> This document describes only the **why** and the **what**. It intentionally excludes implementation details, technical design choices, protocols, deployment architecture, and UI technology decisions.

## 2. Background and Problem Statement

NATS is increasingly used as a foundational messaging and coordination layer in distributed systems. As adoption grows, operators and developers need a single application that makes NATS environments understandable, governable, and safer to operate.

Today, management of NATS-based environments is often fragmented across command-line tools, configuration files, logs, ad hoc scripts, and manual inspection. This creates several problems:

- Limited visibility into the current state of the messaging environment
- Operational complexity when administering multiple NATS capabilities
- Increased risk of mistakes during administrative actions
- Slow troubleshooting and diagnosis
- Inconsistent management experience across Core NATS, JetStream, KV, Object Store, and Services
- Insufficient governance, auditing, and access control for production use

The application is needed to provide a unified management experience that improves situational awareness, reduces operational risk, and enables controlled administration of NATS resources.

## 3. Product Vision

The product shall provide a single management application through which authorized users can inspect, govern, and operate NATS instances and their related resources in a consistent and understandable way.

The product shall help users answer questions such as:

- Which NATS environments exist and what is their status?
- What accounts, users, permissions, and resources are present?
- Which streams, consumers, buckets, objects, and services exist?
- What is happening in the system right now?
- Where are problems occurring?
- What administrative actions are possible and safe?
- Who changed what, when, and why?

## 4. Goals

The application shall aim to achieve the following goals:

1. Provide a unified operational view of NATS capabilities.
2. Reduce the cognitive load of managing NATS environments.
3. Enable safe administration of messaging and storage resources.
4. Improve observability and troubleshooting efficiency.
5. Support controlled usage in development, test, and production environments.
6. Provide auditability and governance for administrative operations.
7. Support both day-to-day operations and occasional administrative tasks.

## 5. Non-Goals

The product is **not** intended to:

- Replace application-specific business dashboards
- Serve as a custom application development platform
- Redesign NATS concepts or abstract them beyond recognition
- Perform business message transformation or business workflow orchestration
- Hide all complexity at the expense of correctness or operator control
- Automate every operational decision without explicit user intent
- Provide infrastructure provisioning as its primary purpose

## 6. Stakeholders and User Types

### 6.1 Operators

Users responsible for monitoring, troubleshooting, and maintaining NATS environments.

### 6.2 Platform Engineers

Users responsible for setting up standards, governance, permissions, and reusable messaging infrastructure.

### 6.3 Developers

Users who need visibility into subjects, streams, consumers, KV buckets, object stores, and services for debugging and validation.

### 6.4 Administrators

Users with elevated permissions who manage connections, access rights, policies, and sensitive operations.

### 6.5 Auditors and Compliance Stakeholders

Users who need evidence of changes, access, operational events, and traceability of administrative actions.

## 7. Scope

The application shall support management of the following capability areas:

| Capability Area | Description |
| --- | --- |
| Environment Management | Connection to one or more NATS environments |
| Core NATS | Visibility and administration |
| JetStream | Visibility and administration |
| Key-Value Store | JetStream KV visibility and administration |
| Object Store | JetStream Object Store visibility and administration |
| NATS Services | Discovery and inspection |
| Access Control | Governance within the management application |
| Audit Logging | User action audit trail |
| Monitoring | Operational monitoring and diagnostics |

## 8. High-Level Functional Capabilities

### 8.1 Environment Management

The application shall allow users to work with one or more NATS environments.

**Why:** Organizations frequently operate multiple environments such as development, test, staging, and production, or multiple regional and tenant-specific clusters.

**What:**

- Users shall be able to register and identify multiple NATS environments.
- Users shall be able to distinguish environments clearly.
- Users shall be able to view the health and availability status of each environment.
- Users shall be able to select an environment and work within its scope.
- Users shall be able to avoid accidental actions in the wrong environment through clear environment context.

## 9. Functional Requirements by Domain

### 9.1 Connection and Context Management

**Purpose:** Users need a clear and trustworthy connection context before performing inspection or administration.

**Requirements:**

- The application shall present the currently selected environment and connection context at all times.
- The application shall allow authorized users to define, view, modify, enable, and disable environment connections.
- The application shall indicate whether a connection is available, degraded, or unavailable.
- The application shall indicate the last known successful interaction with an environment.
- The application shall allow users to test whether an environment is reachable and usable.
- The application shall make it clear when data shown is live, recently refreshed, or potentially stale.
- The application shall support separation of environments so that resources from different environments are not mixed unintentionally.

### 9.2 Core NATS Management

**Purpose:** Core NATS is the foundation for subject-based messaging. Users need visibility into subjects, connections, accounts, subscriptions, and message activity.

**Requirements:**

- The application shall allow users to inspect the overall status of a Core NATS environment.
- The application shall allow users to view connected clients and their relevant metadata.
- The application shall allow users to view accounts and account-level information where available.
- The application shall allow users to inspect active subscriptions and subject usage where available.
- The application shall allow users to explore subjects and subject hierarchies.
- The application shall allow users to observe message traffic characteristics at subject and environment level where available.
- The application shall allow users to publish messages to subjects when authorized.
- The application shall allow users to subscribe to and inspect messages on subjects when authorized.
- The application shall allow users to identify inactive, high-volume, or error-prone messaging areas where such information is available.
- The application shall allow users to inspect request-reply patterns where observable.
- The application shall warn users before performing actions that may affect live traffic.

### 9.3 JetStream Management

**Purpose:** JetStream introduces persistence, replay, retention, and durable consumption. Users need visibility into streams and consumers to operate systems safely.

**Requirements:**

- The application shall allow users to view all available streams within the selected environment and scope.
- The application shall allow users to inspect stream details, state, limits, and usage.
- The application shall allow users to view message counts, storage usage, retention-related state, and other relevant stream statistics.
- The application shall allow users to create, update, and remove streams when authorized.
- The application shall allow users to inspect all consumers associated with a stream.
- The application shall allow users to view consumer state, backlog, delivery state, acknowledgment-related information, and health indicators.
- The application shall allow users to create, update, and remove consumers when authorized.
- The application shall allow users to inspect messages stored in streams where permitted.
- The application shall allow users to search, filter, and browse stream and consumer resources.
- The application shall allow users to identify operational issues such as stalled consumers, growing backlogs, storage pressure, or stream misuse.
- The application shall provide clear warnings and confirmations before destructive operations.
- The application shall allow users to distinguish between observation actions and state-changing actions.

### 9.4 JetStream Key-Value Store Management

**Purpose:** KV buckets are used for distributed configuration, coordination, and state sharing. Users need a practical way to inspect and manage keys and bucket state.

**Requirements:**

- The application shall allow users to view all KV buckets within the selected scope.
- The application shall allow users to inspect bucket details and state.
- The application shall allow users to create, update, and remove KV buckets when authorized.
- The application shall allow users to browse keys within a bucket.
- The application shall allow users to inspect the current value and relevant metadata of a key where permitted.
- The application shall allow users to create, update, and delete keys when authorized.
- The application shall allow users to inspect revision-related information where available.
- The application shall allow users to review recent key activity where available.
- The application shall allow users to search and filter keys.
- The application shall allow users to distinguish deleted, missing, current, and superseded key states where such distinctions are meaningful.
- The application shall help users avoid accidental overwrites or unintended deletion of important coordination data.
- The application shall make clear that KV data may be operationally sensitive.

### 9.5 JetStream Object Store Management

**Purpose:** Object Store is used for storing larger binary or structured objects. Users need visibility into buckets, objects, and their metadata.

**Requirements:**

- The application shall allow users to view all object store buckets within the selected scope.
- The application shall allow users to inspect bucket details and state.
- The application shall allow users to create, update, and remove object store buckets when authorized.
- The application shall allow users to browse objects within a bucket.
- The application shall allow users to inspect object metadata.
- The application shall allow users to upload objects when authorized.
- The application shall allow users to download objects when authorized.
- The application shall allow users to replace or delete objects when authorized.
- The application shall allow users to search and filter objects.
- The application shall allow users to understand object size and other relevant operational characteristics.
- The application shall warn users before downloading, replacing, or deleting sensitive or large objects where appropriate.
- The application shall make it clear when an action may affect downstream users or systems relying on stored objects.

### 9.6 NATS Services Management

**Purpose:** NATS Services expose request-reply based capabilities. Users need a way to discover services and understand their availability and behavior.

**Requirements:**

- The application shall allow users to discover services available in the selected environment where discoverability is supported.
- The application shall allow users to view service identity and descriptive metadata where available.
- The application shall allow users to inspect service status and health indicators where available.
- The application shall allow users to view service endpoints, groups, versions, and other relevant descriptors where available.
- The application shall allow users to understand which services are active, unavailable, degraded, or inconsistent where such information is available.
- The application shall allow users to issue test requests to services when authorized.
- The application shall allow users to inspect service responses for diagnostic purposes where permitted.
- The application shall clearly distinguish service discovery information from authoritative business documentation.
- The application shall warn users before sending test requests that may have side effects.

### 9.7 Resource Navigation, Search, and Discovery

**Purpose:** NATS environments can become large and difficult to navigate. Users need fast discovery across all supported resource types.

**Requirements:**

- The application shall provide search capabilities across relevant resources.
- The application shall allow filtering by resource type, environment, scope, and status.
- The application shall support navigation through hierarchical or grouped views where meaningful.
- The application shall allow users to move from high-level summaries to detailed inspection views.
- The application shall allow users to discover relationships between resources where such relationships are meaningful.
- The application shall support locating resources quickly by name, subject, bucket, stream, consumer, service, or other known identifiers.
- The application shall support bookmarking or otherwise quickly returning to frequently used resources.

### 9.8 Message, Data, and Payload Inspection

**Purpose:** Operational users need to inspect actual data for diagnosis, validation, and support.

**Requirements:**

- The application shall allow authorized users to inspect messages and payloads from relevant NATS resources.
- The application shall allow authorized users to inspect metadata associated with messages, keys, objects, and service requests/responses.
- The application shall preserve clarity between metadata and payload content.
- The application shall support viewing raw content where permitted.
- The application shall support basic payload readability improvements for commonly used structured content formats where possible.
- The application shall make clear when content is truncated, partial, transformed for display, or unavailable.
- The application shall provide safeguards for sensitive or confidential content.
- The application shall allow access to inspection features to be restricted by role or policy.

### 9.9 Administrative Actions and Safeguards

**Purpose:** The application must support administration, but administrative power must be controlled and deliberate.

**Requirements:**

- The application shall support both read-only and state-changing operations.
- The application shall clearly identify when an action changes system state.
- The application shall require explicit confirmation for destructive or high-impact actions.
- The application shall provide clear descriptions of the target resource before execution of sensitive actions.
- The application shall allow actions to be restricted based on user role, policy, or environment classification.
- The application shall prevent or strongly discourage accidental destructive actions.
- The application shall allow organizations to limit which actions are available in specific environments, such as production.
- The application shall record state-changing actions for audit purposes.
- The application shall communicate whether an action succeeded, failed, or completed with warnings.

### 9.10 Monitoring and Operational Visibility

**Purpose:** Users need to identify abnormal conditions and assess the operational state of the environment quickly.

**Requirements:**

- The application shall provide an overview of environment status and relevant operational indicators.
- The application shall provide summaries of key resource health across Core NATS, JetStream, KV, Object Store, and Services.
- The application shall help users detect conditions such as connectivity issues, resource growth, unhealthy consumers, unavailable services, or unusual activity.
- The application shall allow users to drill down from summary views into detailed views.
- The application shall distinguish current state from historical trend information where both are available.
- The application shall make severe or urgent conditions more visible than normal conditions.
- The application shall support operational awareness without requiring users to inspect every resource manually.

### 9.11 Alerts, Events, and Notifications

**Purpose:** Users need to be informed when important conditions occur.

**Requirements:**

- The application shall present operational events and noteworthy conditions relevant to managed environments.
- The application shall allow users to see recent warnings, failures, and significant administrative outcomes.
- The application shall support notification of important conditions to appropriate users or roles.
- The application shall support different severities or priorities for notable conditions.
- The application shall allow users to distinguish between informational events and actionable problems.
- The application shall make it clear whether a condition is active, resolved, or historical where applicable.

### 9.12 Access Control and Authorization

**Purpose:** NATS management often includes highly privileged and sensitive actions. Access must be controlled.

**Requirements:**

- The application shall require user authentication.
- The application shall support differentiated authorization for viewing and administering resources.
- The application shall support role-based or policy-based access distinctions.
- The application shall allow organizations to restrict access by environment, resource type, and action type.
- The application shall support read-only access for users who need visibility without administrative rights.
- The application shall support privileged access for users who are responsible for administration.
- The application shall prevent unauthorized users from seeing or performing restricted operations.
- The application shall ensure that sensitive resource access is governed consistently.
- The application shall make it possible to review which users are permitted to perform which categories of actions.

### 9.13 Auditability and Traceability

**Purpose:** Operational governance and compliance require traceable evidence of user activity.

**Requirements:**

- The application shall record meaningful audit events for relevant user actions.
- The application shall record who performed an action, what action was performed, when it occurred, and against which resource and environment.
- The application shall record the outcome of administrative actions.
- The application shall support audit records for authentication events, authorization-relevant events, configuration changes, and state-changing operations.
- The application shall allow authorized users to inspect audit history.
- The application shall support searching and filtering of audit history.
- The application shall make audit data suitable for operational review and accountability.
- The application shall distinguish system-generated events from user-initiated events where possible.

### 9.14 Multi-Environment and Multi-Tenancy Awareness

**Purpose:** Organizations may manage multiple clusters, tenants, or isolated scopes and must avoid cross-boundary confusion.

**Requirements:**

- The application shall support management of multiple separate environments.
- The application shall clearly display which environment and scope the user is viewing.
- The application shall prevent confusion between similar resource names in different environments.
- The application shall support isolation of data and actions between environments or tenants.
- The application shall allow organizations to define which users may access which environments or scopes.
- The application shall help users avoid executing actions in the wrong context.

### 9.15 Usability and Operator Experience

**Purpose:** Operational tools must reduce friction, not add it.

**Requirements:**

- The application shall use NATS terminology accurately and consistently.
- The application shall make key information understandable to users who are not NATS experts while remaining correct for expert users.
- The application shall favor clarity over hidden behavior.
- The application shall support efficient use for both occasional and frequent users.
- The application shall surface important context close to the actions and data it relates to.
- The application shall support progressive disclosure so users can start from a summary and move into details.
- The application shall provide informative error feedback to users.
- The application shall help users understand the consequences of actions before they execute them.

## 10. Functional Areas in Detail

### 10.1 Dashboards and Overview Views

**Why:** Users need a quick way to understand the current operational state without navigating every individual resource.

**What:**

- The application shall provide overview views for environments.
- The application shall summarize the state of Core NATS, JetStream, KV, Object Store, and Services.
- The application shall surface notable issues, health concerns, and recent changes.
- The application shall provide shortcuts from overview information into detailed resource views.

### 10.2 Resource Detail Views

**Why:** Users need trustworthy detail pages for inspection and action.

**What:**

- The application shall provide detailed views for each supported resource type.
- Each resource view shall present identity, status, relevant metadata, relationships, and permitted actions.
- Resource views shall distinguish static descriptors from changing operational state.
- Resource views shall make it clear when information is incomplete or unavailable.

### 10.3 Destructive Operations

**Why:** Deletion or reset actions may have significant operational impact.

**What:**

- The application shall identify destructive operations explicitly.
- The application shall require deliberate confirmation for destructive operations.
- The application shall present enough context for users to understand what will be affected.
- The application shall make destructive operations auditable.
- The application shall support restricting destructive operations more strongly than read operations.

### 10.4 Read-Only Operational Mode

**Why:** Many users need visibility without modification rights.

**What:**

- The application shall support a read-only usage mode.
- In read-only mode, the application shall allow inspection but not state changes.
- The application shall clearly indicate when the user is operating with read-only permissions.
- The application shall hide, disable, or otherwise prevent unauthorized state-changing actions.

## 11. Cross-Cutting Functional Requirements

### 11.1 Consistency

- The application shall present a consistent management model across supported NATS capability areas.
- Similar actions across streams, consumers, buckets, objects, and services shall be expressed consistently where appropriate.
- The application shall avoid inconsistent naming and behavior across resource types unless the underlying domain requires a difference.

### 11.2 Explainability

- The application shall help users understand what a resource is and why it matters in operational terms.
- The application shall provide contextual guidance or descriptions for specialized NATS concepts where useful.
- The application shall make clear whether displayed data is observed, configured, derived, or inferred.

### 11.3 Safety

- The application shall prioritize safe operation in environments that may carry production workloads.
- The application shall reduce the likelihood of accidental, ambiguous, or irreversible actions.
- The application shall support stronger safeguards for sensitive environments.

### 11.4 Governance

- The application shall support organizational governance through access control, auditability, and operational boundaries.
- The application shall support use in environments where controlled change and accountability are required.

## 12. Data Sensitivity and Privacy-Related Functional Expectations

Although this product manages technical infrastructure rather than business processes, it may expose operationally or commercially sensitive data.

The application shall therefore:

- Allow access to sensitive data to be restricted
- Distinguish low-risk metadata from potentially sensitive payload content
- Support limiting who can inspect messages, values, objects, and service responses
- Ensure that auditability exists for sensitive administrative actions
- Support controlled usage in regulated or security-conscious environments

## 13. Functional Constraints

The product shall adhere to the following functional constraints:

- The product shall preserve the conceptual integrity of NATS rather than inventing a misleading abstraction.
- The product shall not require users to understand implementation details before performing basic inspection tasks.
- The product shall not remove important operational context in the name of simplicity.
- The product shall not blur the boundary between observation and administration.
- The product shall not allow unrestricted destructive behavior without appropriate safeguards.

## 14. Success Criteria

The product will be considered functionally successful when it enables authorized users to do the following effectively:

- Understand the current state of a NATS environment
- Locate relevant messaging, storage, and service resources quickly
- Inspect operational data with confidence
- Perform authorized administrative tasks safely
- Troubleshoot common issues faster than with fragmented tooling
- Maintain separation between environments and scopes
- Demonstrate accountability for administrative actions

## 15. Out of Scope for This Specification

The following are intentionally excluded from this specification:

- Technical architecture
- UI technology stack
- Backend implementation design
- Storage technology choices
- Integration protocol details
- Deployment model
- Performance engineering approach
- Internal domain model design
- API design
- Authentication protocol selection
- Detailed non-functional requirements
- Screen mockups and visual design

## 16. Suggested Structure for Follow-Up Specifications

This functional specification should be followed by separate documents for:

1. Domain model and bounded contexts
2. User roles and authorization matrix
3. Detailed resource-specific behavior
4. Audit event catalog
5. Non-functional requirements
6. UX flows and interaction design
7. Technical architecture and integration design
8. Acceptance criteria and test scenarios

## 17. Summary

This application is needed because NATS environments are powerful but operationally fragmented to manage. The product shall provide a unified, safe, and auditable way to inspect and administer Core NATS, JetStream, KV Store, Object Store, and NATS Services.

Its primary value is not merely convenience. Its value is:

improved visibility,
safer operations,
faster troubleshooting,
better governance,
and consistent management across the NATS ecosystem.

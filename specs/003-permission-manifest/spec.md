# Feature Specification: Application Permission Exposure

**Feature Branch**: `003-permission-manifest`
**Created**: 2026-05-23
**Status**: Draft
**Input**: User description: "Application Permission Exposure Specification"

## Clarifications

### Session 2026-05-23

- Q: What should determine whether the permission manifest endpoint is public or restricted? -> A: Deployment-policy controlled; public or restricted behavior must be explicit per environment.
- Q: What should happen when the current manifest is invalid at retrieval time? -> A: Return the last valid published manifest and record the validation failure.
- Q: Which version field should consumers use for change tracking? -> A: Use only the application version for change tracking.
- Q: How should deprecated permissions appear in the published manifest? -> A: Do not publish deprecated permissions; only active permissions appear.
- Q: How quickly should permission updates appear in discovery responses? -> A: Next retrieval after successful publication.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover Application Permissions (Priority: P1)

As an external consumer responsible for access management, I need to discover the complete permission set owned by an application from a predictable location so that downstream systems can understand what permissions the application publishes.

**Why this priority**: Discovery of the full permission set is the core value of the feature and enables every consumer workflow.

**Independent Test**: Can be fully tested by requesting the well-known permission location and confirming the response contains application metadata, all owned permissions, and human-readable permission descriptions.

**Acceptance Scenarios**:

1. **Given** the application has a valid permission manifest, **When** an external consumer requests `/.well-known/permissions`, **Then** the application returns a successful JSON response containing application metadata and the full permission set.
2. **Given** the manifest contains multiple permissions, **When** an external consumer reviews the response, **Then** each permission includes a unique name and a human-readable description.

---

### User Story 2 - Publish Permission Metadata for Administration (Priority: P2)

As an administrator or policy tooling consumer, I need permission metadata such as categories and application identity so that permissions can be presented, grouped, and reviewed without relying on internal application knowledge.

**Why this priority**: Metadata makes the manifest useful for administration and policy review beyond raw permission names.

**Independent Test**: Can be tested by retrieving a manifest with categorized permissions and confirming the application identity and grouping metadata are present where defined.

**Acceptance Scenarios**:

1. **Given** permissions are organized by category, **When** the manifest is retrieved, **Then** category values are included with the relevant permissions.
2. **Given** the application publishes an application version, **When** the manifest is retrieved, **Then** the version is included as application metadata without changing the required payload shape.

---

### User Story 3 - Reflect Permission Changes (Priority: P3)

As an application owner, I need updates to the permission manifest to be reflected in future discovery responses so that consumers can detect additions, removals, and renames without tight coupling to IAM internals.

**Why this priority**: Keeping the published manifest current supports ongoing permission lifecycle management after initial discovery.

**Independent Test**: Can be tested by updating the manifest source, completing successful publication, and confirming the next retrieval returns the updated permission set.

**Acceptance Scenarios**:

1. **Given** a new permission is added to the manifest source and successfully published, **When** the manifest is retrieved next, **Then** the new permission appears with its description and any defined category.
2. **Given** a permission is removed from the manifest source and successfully published, **When** the manifest is retrieved next, **Then** the removed permission no longer appears in the active permission list.
3. **Given** a permission is being renamed, **When** the manifest is prepared for publication, **Then** the new active permission is represented separately and the old permission is removed from the published permission list when it is no longer active.

### Edge Cases

- The current manifest source is unavailable or invalid when a consumer requests the well-known location.
- No prior valid published manifest exists when the current manifest is unavailable or invalid.
- The manifest contains duplicate permission names.
- The manifest omits required application metadata or permission descriptions.
- The endpoint is deployed without an explicit public-or-restricted exposure policy for the environment.
- A permission update occurs while consumers are retrieving the manifest.
- Optional metadata is present that older consumers do not recognize.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST expose a discoverable permission manifest at `/.well-known/permissions`.
- **FR-002**: The permission manifest location MUST support retrieval through a read-only request.
- **FR-003**: The application MUST return a JSON manifest when the permission manifest is available.
- **FR-004**: The manifest MUST include application metadata with at least a stable application identifier and a human-readable application name.
- **FR-005**: The manifest MAY include an application version for change tracking.
- **FR-006**: The manifest MUST include the full set of active permissions owned and published by the application.
- **FR-007**: Each permission MUST include a permission name and a human-readable description.
- **FR-008**: Each permission MAY include a category for administration grouping.
- **FR-009**: The manifest payload shape MUST remain stable across compatible versions so existing consumers can continue reading required fields.
- **FR-010**: The application MUST validate the manifest before publishing it as available.
- **FR-011**: Manifest validation MUST reject missing required application metadata, missing permission names, missing permission descriptions, and duplicate permission names.
- **FR-012**: Permission names SHOULD follow the `{action}:{resource}` naming pattern.
- **FR-013**: Application identifiers SHOULD use kebab-case.
- **FR-014**: When the current manifest cannot be validated or retrieved, the application MUST return the last valid published manifest when one exists and MUST make the failure observable to operators.
- **FR-015**: When no prior valid manifest exists, the application MUST fail retrieval safely without returning malformed or unvalidated permission data.
- **FR-016**: The published manifest MUST exclude deprecated or inactive permissions.
- **FR-017**: The next manifest retrieval after successful publication MUST reflect the latest successfully published manifest after permissions are added, removed, or renamed.
- **FR-018**: The manifest MUST NOT include internal-only metadata that is not intended for external consumers.
- **FR-019**: Each deployment environment MUST explicitly declare whether manifest retrieval is public or restricted; if restricted, the application MUST enforce approved service-to-service access controls before returning manifest content.
- **FR-020**: The application MUST emit structured operational records for manifest retrieval and manifest validation failures.
- **FR-021**: The feature scope MUST be limited to application-owned publication and discovery; IAM-side import, approval, assignment, and policy enforcement behavior are out of scope.

### Key Entities *(include if feature involves data)*

- **Permission Manifest**: The published document that contains application metadata, the application's owned permissions, and optional non-breaking metadata for discovery.
- **Application Metadata**: Identity information for the publishing application, including stable identifier, display name, and optional version.
- **Permission**: A single active application-owned authorization capability, identified by name and described in human-readable language; may include category metadata.
- **Manifest Retrieval**: A consumer request to discover the current permission manifest and the observable result recorded for operations.
- **Manifest Validation Result**: The outcome of checking the manifest for required shape, unique permission names, and absence of restricted internal metadata before publication.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of successful manifest retrievals include application metadata and a permissions list that satisfies the required manifest shape.
- **SC-002**: 100% of published permissions include a unique name and a non-empty human-readable description.
- **SC-003**: 95% of consumers can retrieve and parse the manifest without prior application-specific knowledge during discovery testing.
- **SC-004**: Permission additions, removals, and renames are visible on the next discovery response after successful publication.
- **SC-005**: Invalid manifests are detected before successful publication in 100% of validation test cases covering missing required fields and duplicate permission names.
- **SC-006**: 100% of invalid manifest updates preserve the last valid published manifest for retrieval when one exists.
- **SC-007**: Administration reviewers can identify the publishing application and understand the purpose of each permission from the manifest alone in user acceptance testing.

## Assumptions

- The application is the authoritative owner of the permissions it publishes.
- The manifest represents permissions intended for external discovery and excludes internal-only implementation details.
- The well-known permission location is part of the external contract for this feature because discoverability is the primary requirement.
- Manifest exposure is controlled by deployment policy, and each environment must explicitly declare whether retrieval is public or restricted; when restricted, trusted service access controls are handled by the hosting environment or platform policy.
- Optional metadata such as category details must be additive and non-breaking.
- IAM import, approval, assignment, and enforcement workflows are intentionally outside this feature's scope.

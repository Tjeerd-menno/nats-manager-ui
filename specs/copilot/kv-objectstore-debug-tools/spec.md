# Feature Specification: KV and Object Store Debug Tools

**Feature Branch**: `copilot/kv-objectstore-debug-tools`  
**Created**: 2026-04-26  
**Status**: Draft  
**Input**: User description: "Create the next NATS Manager UI extension plan feature specification for KV and Object Store debug tools, using existing SpecKit conventions and default assumptions for supported NATS versions, safe metadata, ephemeral payload inspection, production safeguards, and in-app-first alerts."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse KV and Object Store resources safely (Priority: P1)

An operator opens the debug tools and browses Key-Value buckets, keys, Object Store buckets, and objects with metadata, state, size, revisions, and freshness indicators without exposing values by default.

**Why this priority**: Safe read-only browsing enables debugging configuration and object state with minimal risk.

**Independent Test**: Can be tested by opening environments with KV/Object Store data and confirming resources are searchable and values/content are masked by default.

**Acceptance Scenarios**:

1. **Given** KV buckets exist, **When** the user opens the KV browser, **Then** buckets, keys, key state, revision, and last modification metadata are displayed when available.
2. **Given** Object Store buckets exist, **When** the user opens the Object Store browser, **Then** buckets, objects, size, digest/checksum metadata, and last modification metadata are displayed when available.
3. **Given** a value or object may contain sensitive content, **When** the resource is displayed, **Then** content is masked until an authorized reveal action is performed.

---

### User Story 2 - Inspect revisions and object metadata (Priority: P2)

A developer inspects KV revisions or Object Store object metadata to determine when a configuration changed, whether tombstones exist, and whether object metadata matches expected values.

**Why this priority**: Most debugging can be completed using metadata and revision history without mutating data or revealing full content.

**Independent Test**: Can be tested by selecting a KV key with history and an object with metadata and verifying revision, deletion, and metadata details are shown accurately.

**Acceptance Scenarios**:

1. **Given** a KV key has multiple revisions, **When** the user opens revision history, **Then** revisions are ordered and show operation type, revision number, timestamp, and masked value preview when available.
2. **Given** a KV key is deleted or tombstoned, **When** the key appears in search results, **Then** its state is clearly labeled.
3. **Given** an object has metadata, **When** the object detail opens, **Then** size, digest/checksum, modification time, and safe user metadata are visible.

---

### User Story 3 - Perform guarded debug mutations (Priority: P3)

An authorized user updates a KV key, deletes a key, uploads/replaces an object, or deletes an object only after reviewing impact warnings, overwrite safeguards, and environment policy.

**Why this priority**: Mutations are necessary for recovery but must be lower priority than safe inspection because they can alter application behavior.

**Independent Test**: Can be tested in a non-production environment by updating a test key or object and confirming confirmation, outcome, and event/audit details.

**Acceptance Scenarios**:

1. **Given** an authorized user edits a KV key, **When** they save, **Then** the confirmation shows environment, bucket, key, expected revision when available, and overwrite warning.
2. **Given** an authorized user replaces an object, **When** they confirm, **Then** the confirmation shows environment, bucket, object name, size, and downstream impact warning.
3. **Given** production mutations are restricted, **When** the user attempts a destructive action, **Then** the action is blocked or requires explicitly permitted administrative policy.

---

### Edge Cases

- What happens when a KV key has many revisions? The view pages or bounds revision history and keeps current state visible.
- What happens when an object is too large to preview? The UI shows metadata and size, and content reveal/download follows configured limits.
- What happens when a key changes between opening and saving? The user is warned about a revision conflict and must refresh or explicitly retry.
- What happens when content is binary or not displayable? The UI shows type/size metadata and avoids corrupting content.
- What happens when KV/Object Store is disabled? The feature shows a clear disabled-state message for the selected environment.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-KVO-001**: System MUST provide KV and Object Store debug tools scoped to the selected environment.
- **FR-KVO-002**: System MUST list KV buckets and keys with safe metadata including state, revision, operation type, size when available, and last modified time.
- **FR-KVO-003**: System MUST list Object Store buckets and objects with safe metadata including name, size, digest/checksum, modified time, and metadata labels when available.
- **FR-KVO-004**: System MUST allow users to search and filter KV keys and objects by bucket, name, state, and metadata.
- **FR-KVO-005**: System MUST mask KV values and object content by default and require authorization plus explicit user action to reveal content.
- **FR-KVO-006**: System MUST keep value and object content inspection ephemeral and avoid persistence unless a future specification explicitly requires it.
- **FR-KVO-007**: System MUST display KV revision history and deletion/tombstone state when available.
- **FR-KVO-008**: System MUST allow guarded KV/object mutations only for authorized users and only after explicit confirmation.
- **FR-KVO-009**: System MUST warn before overwriting, deleting, uploading, or replacing coordination-sensitive KV/Object Store data.
- **FR-KVO-010**: System MUST enforce production safeguards so live content reveal and destructive actions are disabled by default unless an administrator explicitly permits them.
- **FR-KVO-011**: System MUST record user-initiated KV/Object Store mutations and content reveal actions as auditable operational events.
- **FR-KVO-012**: System MUST support currently maintained NATS 2.x releases and target NATS 2.10+ KV/Object Store behavior.

### Key Entities

- **KV Bucket**: A Key-Value container with configuration, known key count, history settings, and observed state.
- **KV Key Revision**: A versioned key record with revision number, operation type, timestamp, size, and optional masked value preview.
- **Object Store Bucket**: An object container with configuration, object count, storage usage, and observed state.
- **Stored Object**: An Object Store item with name, size, digest/checksum, metadata, and modification information.
- **Content Inspection Session**: A bounded, ephemeral session for revealing KV value or object content.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-KVO-001**: Users can locate a known key or object within 30 seconds in environments with up to 10,000 combined keys/objects.
- **SC-KVO-002**: 100% of value/object content is masked by default before explicit reveal.
- **SC-KVO-003**: 100% of guarded mutations require confirmation showing environment, bucket, target name, and impact warning.
- **SC-KVO-004**: Revision conflicts are detected and communicated before overwrite in 100% of conflict validation cases.
- **SC-KVO-005**: No revealed KV value or object content remains available after the inspection session ends.

## Assumptions

- Supported NATS servers are currently maintained NATS 2.x releases, targeting NATS 2.10+.
- KV Store and Object Store are available only when JetStream-backed capabilities are enabled for the selected environment.
- Multi-account/operator JWT support is future work unless monitoring/discovery endpoints expose safe metadata.
- Payload/value/object content inspection is ephemeral and must not be persisted unless a future spec explicitly requires it.
- Production environments disable live tapping, replay, payload reveal, and destructive debug actions by default unless an administrator explicitly permits them.
- Alerts generated from KV/Object Store issues are in-app first; external notifications are future work.

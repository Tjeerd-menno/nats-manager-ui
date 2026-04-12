# Data Model: NATS Admin Application

**Branch**: `001-nats-admin-app`
**Date**: 2026-04-06
**Source**: [spec.md](spec.md) key entities + [research.md](research.md) persistence decisions

---

## Persistence Boundary

The data model separates **application-owned entities** (persisted in SQLite via EF Core) from **observed resources** (read live from NATS through adapters). Application-owned entities follow DDD aggregate patterns. Observed resources are represented as read-only DTOs in the application layer.

---

## Application-Owned Entities (SQLite)

### Environment (Aggregate Root)

Represents a registered NATS deployment that the application can connect to.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key, generated on creation |
| Name | string (1–100) | Required, unique, display name |
| Description | string (0–500) | Optional |
| ServerUrl | string (1–2048) | Required, validated URL/host format |
| CredentialType | enum | None, Token, UserPassword, NKey, CredsFile |
| CredentialReference | string (0–500) | Encrypted reference to stored credential |
| IsEnabled | bool | Default true; disabled environments are not connected |
| IsProduction | bool | Default false; production environments have stricter safeguards |
| ConnectionStatus | enum | Unknown, Available, Degraded, Unavailable |
| LastSuccessfulContact | DateTimeOffset? | Null if never connected successfully |
| CreatedAt | DateTimeOffset | Set on creation |
| UpdatedAt | DateTimeOffset | Set on every modification |

**Invariants**:
- Name must be unique across all environments
- ServerUrl must be a valid NATS connection target
- Credential changes must be audited

**State transitions**:
- `Unknown` → `Available` (first successful connection test)
- `Available` → `Degraded` (partial connectivity or slow response)
- `Available` → `Unavailable` (connection failure)
- `Degraded` → `Available` (recovery detected)
- `Unavailable` → `Available` (connection restored)
- Any state → `Unknown` (environment disabled and re-enabled)

---

### User (Aggregate Root)

Represents an authenticated person who can use the application.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| Username | string (1–100) | Required, unique |
| DisplayName | string (1–200) | Required |
| PasswordHash | string | Hashed with PBKDF2/Argon2 |
| IsActive | bool | Default true; inactive users cannot log in |
| CreatedAt | DateTimeOffset | Set on creation |
| LastLoginAt | DateTimeOffset? | Updated on successful authentication |

**Invariants**:
- Username must be unique
- Password must meet minimum complexity requirements
- Deactivation must be audited

---

### Role (Entity)

Defines a set of permissions assignable to users.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| Name | string (1–50) | Required, unique. Predefined: ReadOnly, Operator, Administrator, Auditor |
| Description | string (0–500) | Human-readable purpose description |

**Predefined roles**:
- **ReadOnly**: View all resources, no state-changing actions
- **Operator**: View + modify resources, destructive actions blocked in production environments
- **Administrator**: Full access including destructive actions (with confirmation) and user management
- **Auditor**: View resources + view audit history, no modifications

---

### UserRoleAssignment (Entity)

Associates a user with a role, optionally scoped to a specific environment.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| UserId | Guid | FK → User.Id, required |
| RoleId | Guid | FK → Role.Id, required |
| EnvironmentId | Guid? | FK → Environment.Id, null = all environments |
| AssignedAt | DateTimeOffset | Set on creation |
| AssignedBy | Guid | FK → User.Id (the admin who assigned) |

**Invariants**:
- A user may have different roles in different environments
- A user may have one global role (EnvironmentId = null) and overriding per-environment roles
- Role assignment changes must be audited

---

### AuditEvent (Aggregate Root)

Immutable record of a user or system action.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| Timestamp | DateTimeOffset | UTC, set on creation |
| ActorId | Guid? | FK → User.Id, null for system-generated events |
| ActorName | string (0–200) | Denormalized for query performance |
| ActionType | enum | Create, Update, Delete, TestInvoke, Publish, Subscribe, Login, Logout, PermissionChange |
| ResourceType | enum | Environment, Stream, Consumer, KvBucket, KvKey, ObjectBucket, Object, Service, User, Role |
| ResourceId | string (0–500) | Identifier of the affected resource |
| ResourceName | string (0–500) | Denormalized display name |
| EnvironmentId | Guid? | FK → Environment.Id, null for cross-environment actions |
| Outcome | enum | Success, Failure, Warning |
| Details | string (JSON) | Action-specific context (e.g., old/new values for updates) |
| Source | enum | UserInitiated, SystemGenerated |

**Invariants**:
- AuditEvents are append-only; they cannot be modified or deleted by the application
- Timestamp must be UTC

---

### Bookmark (Entity)

Allows users to save quick-access references to frequently used NATS resources.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| UserId | Guid | FK → User.Id, required |
| EnvironmentId | Guid | FK → Environment.Id, required |
| ResourceType | enum | Stream, Consumer, KvBucket, KvKey, ObjectBucket, Object, Service |
| ResourceId | string (1–500) | External resource identifier |
| DisplayName | string (1–200) | Human-readable label |
| CreatedAt | DateTimeOffset | Set on creation |

**Invariants**:
- A user cannot bookmark the same resource twice (unique on UserId + EnvironmentId + ResourceType + ResourceId)

---

### UserPreference (Entity)

Stores per-user UI preferences.

| Field | Type | Rules |
|-------|------|-------|
| Id | Guid | Primary key |
| UserId | Guid | FK → User.Id, required |
| Key | string (1–100) | Preference identifier (e.g., "defaultEnvironmentId", "theme", "listPageSize") |
| Value | string (0–2000) | Preference value |

**Invariants**:
- Key must be unique per user

---

## Observed Resources (Read from NATS — Not Persisted)

These entities are read-only DTOs populated by infrastructure adapters querying live NATS state. They are NOT stored in SQLite.

### NatsServerInfo

| Field | Type | Source |
|-------|------|--------|
| ServerId | string | `$SYS.REQ.SERVER.PING` |
| ServerName | string | Server info |
| Version | string | Server info |
| JetStreamEnabled | bool | Server info |
| MaxPayload | long | Server info |
| ClientCount | int | Server varz |
| Uptime | TimeSpan | Server varz |

### StreamInfo

| Field | Type | Source |
|-------|------|--------|
| Name | string | JetStream API |
| Subjects | string[] | Stream configuration |
| RetentionPolicy | enum (Limits, Interest, WorkQueue) | Stream configuration |
| StorageType | enum (File, Memory) | Stream configuration |
| MaxBytes | long | Stream configuration |
| MaxMsgs | long | Stream configuration |
| MaxAge | TimeSpan | Stream configuration |
| MessageCount | long | Stream state |
| ByteCount | long | Stream state |
| ConsumerCount | int | Stream state |
| FirstSequence | long | Stream state |
| LastSequence | long | Stream state |
| Replicas | int | Stream configuration |

### ConsumerInfo

| Field | Type | Source |
|-------|------|--------|
| Name | string | JetStream API |
| StreamName | string | Consumer configuration |
| DurableName | string? | Consumer configuration |
| DeliverPolicy | enum | Consumer configuration |
| AckPolicy | enum | Consumer configuration |
| FilterSubject | string? | Consumer configuration |
| NumPending | long | Consumer state (backlog) |
| NumAckPending | long | Consumer state |
| NumRedelivered | long | Consumer state |
| LastDelivered | StreamSequencePair | Consumer state |
| IsHealthy | bool | Derived: NumPending < threshold && no stall indicators |

### KvBucketInfo

| Field | Type | Source |
|-------|------|--------|
| BucketName | string | KV API |
| History | int | Bucket configuration (max history per key) |
| MaxBytes | long | Bucket configuration |
| MaxValueSize | int | Bucket configuration |
| TTL | TimeSpan? | Bucket configuration |
| KeyCount | long | Bucket state |
| ByteCount | long | Bucket state |

### KvEntry

| Field | Type | Source |
|-------|------|--------|
| Key | string | KV API |
| Value | byte[] | KV API |
| Revision | long | KV entry metadata |
| Operation | enum (Put, Delete, Purge) | KV entry metadata |
| CreatedAt | DateTimeOffset | KV entry metadata |

### ObjectBucketInfo

| Field | Type | Source |
|-------|------|--------|
| BucketName | string | Object Store API |
| Description | string? | Bucket metadata |
| MaxChunkSize | int | Bucket configuration |
| MaxBytes | long | Bucket configuration |
| ObjectCount | long | Bucket state |
| ByteCount | long | Bucket state |

### ObjectInfo

| Field | Type | Source |
|-------|------|--------|
| Name | string | Object Store API |
| Description | string? | Object metadata |
| Size | long | Object metadata |
| Chunks | int | Object metadata |
| Digest | string? | Object metadata (SHA-256) |
| ModifiedAt | DateTimeOffset | Object metadata |
| Headers | Dictionary<string,string> | Object metadata |

### ServiceInfo

| Field | Type | Source |
|-------|------|--------|
| Name | string | `$SRV.INFO` response |
| Id | string | Service instance ID |
| Version | string | Service metadata |
| Description | string? | Service metadata |
| Endpoints | ServiceEndpoint[] | Service metadata |
| IsAvailable | bool | Derived from `$SRV.PING` |

### ServiceEndpoint

| Field | Type | Source |
|-------|------|--------|
| Name | string | Endpoint metadata |
| Subject | string | Endpoint subject |
| QueueGroup | string? | Endpoint queue group |
| NumRequests | long | `$SRV.STATS` |
| NumErrors | long | `$SRV.STATS` |
| AverageProcessingTime | TimeSpan | `$SRV.STATS` |

---

## Entity Relationships

```text
User ──1:N──▶ UserRoleAssignment ◀──N:1── Role
                    │
                    └──0..1:1──▶ Environment (scope)

User ──1:N──▶ Bookmark ──N:1──▶ Environment
User ──1:N──▶ UserPreference
User ──0..N──▶ AuditEvent (as actor)

Environment ──1:N──▶ AuditEvent (as scope)
Environment ──1:N──▶ Bookmark

── Live from NATS (per environment) ──
Environment ──*──▶ NatsServerInfo
Environment ──*──▶ StreamInfo ──*──▶ ConsumerInfo
Environment ──*──▶ KvBucketInfo ──*──▶ KvEntry
Environment ──*──▶ ObjectBucketInfo ──*──▶ ObjectInfo
Environment ──*──▶ ServiceInfo ──*──▶ ServiceEndpoint
```

---

## Validation Rules Summary

| Entity | Rule | Enforcement Layer |
|--------|------|-------------------|
| Environment.Name | Non-empty, max 100 chars, unique | Application + Database |
| Environment.ServerUrl | Valid URL/host format | Application |
| Environment.CredentialReference | Encrypted at rest | Infrastructure |
| User.Username | Non-empty, max 100, unique | Application + Database |
| User.PasswordHash | Argon2/PBKDF2 with salt | Infrastructure |
| AuditEvent | Immutable after creation | Domain invariant |
| Bookmark | Unique per user+environment+resource | Application + Database |
| KvEntry updates | Revision-based optimistic concurrency | NATS KV adapter |
| All destructive commands | Authorization + confirmation required | Application (command handler) |

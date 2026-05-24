# Data Model: Application Permission Exposure

## PermissionManifest

Published document returned by `GET /.well-known/permissions`.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `application` | `ApplicationMetadata` | Yes | Must be valid |
| `permissions` | `PermissionDefinition[]` | Yes | Must contain active permissions only; duplicate names rejected |

Relationships:

- Owns exactly one `ApplicationMetadata`.
- Owns zero or more active `PermissionDefinition` items.
- Is produced by `PermissionManifestRegistry` and published through `PermissionManifestPublisher`.

## ApplicationMetadata

Identity information for the publishing application.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `id` | string | Yes | Non-empty; kebab-case expected |
| `name` | string | Yes | Non-empty human-readable name |
| `version` | string | No | Optional application version; no separate manifest version exists |

## PermissionDefinition

An active application-owned authorization capability.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `name` | string | Yes | Non-empty; unique within one manifest; `{aggregate-resource}:{action}` expected |
| `description` | string | Yes | Non-empty human-readable description |
| `category` | string | No | Optional non-empty group label when present |

Rules:

- Deprecated and inactive permissions are not included.
- Permission names are case-sensitive for duplicate detection after trimming whitespace.
- Permission names should use lowercase kebab-case resource/action naming, for example `environments:read` or `jetstream-streams:write`.
- Concrete permission names do not include wildcard entries; consumers can derive supported wildcard scopes such as `environments:*` or `jetstream-streams:*`.

## PermissionManifestPublicationState

In-memory state used to serve validated manifests safely.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `lastValidManifest` | `PermissionManifest?` | No | Set after successful validation |
| `lastValidatedAt` | timestamp | No | Set after each validation attempt |
| `lastFailureReason` | string? | No | Set after validation or source failure |
| `status` | enum | Yes | `Valid`, `InvalidWithFallback`, or `InvalidNoFallback` |

State transitions:

```text
Uninitialized
  -> Valid                when startup validation succeeds
  -> InvalidNoFallback    when startup validation fails

Valid
  -> Valid                when a later manifest validates successfully
  -> InvalidWithFallback  when a later manifest fails validation

InvalidWithFallback
  -> Valid                when a later manifest validates successfully
  -> InvalidWithFallback  when failures continue and lastValidManifest exists

InvalidNoFallback
  -> Valid                when a later manifest validates successfully
  -> InvalidNoFallback    when failures continue and no lastValidManifest exists
```

Retrieval behavior:

- `Valid`: return `lastValidManifest` with success.
- `InvalidWithFallback`: return `lastValidManifest`, emit structured validation-failure log, and mark response source as last-valid.
- `InvalidNoFallback`: return a safe failure response without permission data.

## PermissionManifestOptions

Deployment configuration owned by the web layer.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `exposureMode` | enum | Yes | Must be `Public` or `Restricted`; no implicit default in production |
| `restrictedAccessKeyHash` | string? | Conditional | Required when `exposureMode` is `Restricted` |
| `applicationId` | string | Yes | Non-empty; kebab-case expected |
| `applicationName` | string | Yes | Non-empty |
| `applicationVersion` | string? | No | Optional; may fall back to assembly informational version |

Rules:

- Restricted mode requires a service access key check before returning manifest content.
- Public mode must not require session authentication.
- All modes must log manifest retrieval attempts with exposure mode and result status.

## ManifestRetrievalResult

Application result consumed by the HTTP endpoint.

| Field | Type | Required | Validation |
|-------|------|----------|------------|
| `manifest` | `PermissionManifest?` | Conditional | Required for successful retrieval |
| `source` | enum | Yes | `Current` or `LastValid` for successful retrievals |
| `failureReason` | string? | Conditional | Required for safe failure responses |

Rules:

- Successful retrievals always serialize `PermissionManifest`.
- Safe failures serialize problem details, not partial manifests.

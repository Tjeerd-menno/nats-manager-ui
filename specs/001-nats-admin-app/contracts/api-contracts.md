# API Contracts: NATS Admin Application

**Branch**: `001-nats-admin-app`
**Date**: 2026-04-06
**Style**: REST via ASP.NET Core Minimal APIs
**Format**: JSON request/response bodies, `application/json` content type
**Errors**: RFC 9457 `ProblemDetails` for all error responses

---

## Common Patterns

### Pagination

All list endpoints support pagination via query parameters:

```
?page=1&pageSize=50&sortBy=name&sortOrder=asc
```

Response wrapper:

```json
{
  "items": [...],
  "page": 1,
  "pageSize": 50,
  "totalItems": 342,
  "totalPages": 7
}
```

### Data Freshness

All responses that include NATS-observed data include a freshness header:

```
X-Data-Freshness: live|recent|stale
X-Data-Timestamp: 2026-04-06T12:00:00Z
```

### Error Response

All errors use RFC 9457 ProblemDetails:

```json
{
  "type": "https://natsmanager.local/errors/environment-unreachable",
  "title": "Environment Unreachable",
  "status": 503,
  "detail": "Could not connect to environment 'production-us-east' within 10 seconds.",
  "instance": "/api/environments/abc-123/test"
}
```

---

## 1. Environments — `/api/environments`

### `GET /api/environments`

List all registered environments.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "items": [
    {
      "id": "guid",
      "name": "production-us-east",
      "description": "US East production cluster",
      "isEnabled": true,
      "isProduction": true,
      "connectionStatus": "Available",
      "lastSuccessfulContact": "2026-04-06T12:00:00Z"
    }
  ]
}
```

### `POST /api/environments`

Register a new environment.

**Authorization**: Administrator.

**Request**:
```json
{
  "name": "production-us-east",
  "description": "US East production cluster",
  "serverUrl": "nats://nats.prod.internal:4222",
  "credentialType": "Token",
  "credential": "secret-token-value",
  "isProduction": true
}
```

**Response** `201 Created`:
```json
{
  "id": "guid",
  "name": "production-us-east"
}
```

### `GET /api/environments/{id}`

Get environment details.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "id": "guid",
  "name": "production-us-east",
  "description": "US East production cluster",
  "serverUrl": "nats://nats.prod.internal:4222",
  "credentialType": "Token",
  "isEnabled": true,
  "isProduction": true,
  "connectionStatus": "Available",
  "lastSuccessfulContact": "2026-04-06T12:00:00Z",
  "serverInfo": {
    "serverId": "NCXYZ...",
    "serverName": "nats-prod-1",
    "version": "2.11.0",
    "jetStreamEnabled": true,
    "maxPayload": 1048576,
    "clientCount": 42,
    "uptime": "72:15:30"
  }
}
```

### `PUT /api/environments/{id}`

Update environment configuration.

**Authorization**: Administrator.

**Request**:
```json
{
  "name": "production-us-east",
  "description": "Updated description",
  "serverUrl": "nats://nats.prod.internal:4222",
  "credentialType": "Token",
  "credential": "new-token",
  "isEnabled": true,
  "isProduction": true
}
```

**Response** `200 OK`.

### `DELETE /api/environments/{id}`

Remove an environment registration.

**Authorization**: Administrator.

**Response** `204 No Content`.

### `POST /api/environments/{id}/test`

Test connectivity to an environment.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "reachable": true,
  "latencyMs": 12,
  "serverVersion": "2.11.0",
  "jetStreamAvailable": true
}
```

---

## 2. Core NATS — `/api/environments/{envId}/core`

### `GET /api/environments/{envId}/core/status`

Get Core NATS environment status.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "serverId": "NCXYZ...",
  "serverName": "nats-prod-1",
  "version": "2.11.0",
  "jetStreamEnabled": true,
  "clientCount": 42,
  "subscriptionCount": 128,
  "uptime": "72:15:30",
  "maxPayload": 1048576
}
```

### `GET /api/environments/{envId}/core/clients`

List connected clients.

**Authorization**: Any authenticated user.

**Response** `200 OK`: Paginated list of client info objects.

### `GET /api/environments/{envId}/core/subjects`

List active subjects and hierarchy.

**Authorization**: Any authenticated user.

**Query**: `?prefix=orders.>` (optional subject filter)

**Response** `200 OK`:
```json
{
  "items": [
    {
      "subject": "orders.created",
      "subscriptionCount": 3,
      "messagesPerSecond": 15.2
    }
  ]
}
```

### `POST /api/environments/{envId}/core/publish`

Publish a message to a subject.

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "subject": "test.subject",
  "payload": "base64-encoded-content",
  "headers": { "Content-Type": "application/json" }
}
```

**Response** `202 Accepted`.

### `POST /api/environments/{envId}/core/subscribe`

Subscribe to a subject for live message inspection (returns a subscription ID for polling).

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "subject": "orders.>",
  "maxMessages": 100,
  "timeoutSeconds": 30
}
```

**Response** `200 OK`:
```json
{
  "subscriptionId": "guid",
  "subject": "orders.>"
}
```

### `GET /api/environments/{envId}/core/subscriptions/{subId}/messages`

Poll messages from an active inspection subscription.

**Authorization**: Operator or Administrator.

**Response** `200 OK`:
```json
{
  "messages": [
    {
      "subject": "orders.created",
      "payload": "base64-content",
      "headers": {},
      "timestamp": "2026-04-06T12:01:00Z",
      "size": 256
    }
  ],
  "remaining": 85
}
```

---

## 3. JetStream Streams — `/api/environments/{envId}/jetstream/streams`

### `GET /api/environments/{envId}/jetstream/streams`

List all streams.

**Authorization**: Any authenticated user.

**Query**: `?search=orders&sortBy=name&page=1&pageSize=50`

**Response** `200 OK`: Paginated list of stream summaries.
```json
{
  "items": [
    {
      "name": "ORDERS",
      "subjects": ["orders.>"],
      "retentionPolicy": "Limits",
      "storageType": "File",
      "messageCount": 15420,
      "byteCount": 5242880,
      "consumerCount": 3,
      "maxBytes": 104857600,
      "maxAge": "168:00:00"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 12,
  "totalPages": 1
}
```

### `GET /api/environments/{envId}/jetstream/streams/{name}`

Get stream details.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "name": "ORDERS",
  "subjects": ["orders.>"],
  "retentionPolicy": "Limits",
  "storageType": "File",
  "maxBytes": 104857600,
  "maxMsgs": -1,
  "maxAge": "168:00:00",
  "replicas": 3,
  "messageCount": 15420,
  "byteCount": 5242880,
  "consumerCount": 3,
  "firstSequence": 1,
  "lastSequence": 15420,
  "consumers": [
    {
      "name": "order-processor",
      "numPending": 42,
      "isHealthy": true
    }
  ]
}
```

### `POST /api/environments/{envId}/jetstream/streams`

Create a new stream.

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "name": "ORDERS",
  "subjects": ["orders.>"],
  "retentionPolicy": "Limits",
  "storageType": "File",
  "maxBytes": 104857600,
  "maxMsgs": -1,
  "maxAge": "168:00:00",
  "replicas": 3
}
```

**Response** `201 Created`.

### `PUT /api/environments/{envId}/jetstream/streams/{name}`

Update stream configuration.

**Authorization**: Operator or Administrator.

**Request**: Same shape as create (fields that can be updated).

**Response** `200 OK`.

### `DELETE /api/environments/{envId}/jetstream/streams/{name}`

Delete a stream.

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true` (required for destructive actions).

**Response** `204 No Content`.

### `POST /api/environments/{envId}/jetstream/streams/{name}/purge`

Purge all messages from a stream (retains stream configuration and consumers).

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true` (required for destructive actions).

**Request** (optional filter):
```json
{
  "subject": "orders.legacy.>",
  "keepMessages": 0
}
```

**Response** `200 OK`:
```json
{
  "purgedMessages": 15420
}
```

### `GET /api/environments/{envId}/jetstream/streams/{name}/messages`

Browse messages in a stream.

**Authorization**: Any authenticated user (payload access may be restricted by role).

**Query**: `?startSequence=100&count=20`

**Response** `200 OK`:
```json
{
  "messages": [
    {
      "sequence": 100,
      "subject": "orders.created",
      "timestamp": "2026-04-06T11:00:00Z",
      "size": 256,
      "headers": {},
      "payload": "base64-content"
    }
  ]
}
```

---

## 4. JetStream Consumers — `/api/environments/{envId}/jetstream/streams/{stream}/consumers`

### `GET /api/environments/{envId}/jetstream/streams/{stream}/consumers`

List consumers for a stream.

**Authorization**: Any authenticated user.

**Response** `200 OK`: Paginated list of consumer summaries.

### `GET /api/environments/{envId}/jetstream/streams/{stream}/consumers/{name}`

Get consumer details.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "name": "order-processor",
  "streamName": "ORDERS",
  "durableName": "order-processor",
  "deliverPolicy": "All",
  "ackPolicy": "Explicit",
  "filterSubject": "orders.created",
  "numPending": 42,
  "numAckPending": 5,
  "numRedelivered": 2,
  "lastDelivered": {
    "consumerSequence": 15378,
    "streamSequence": 15378
  },
  "isHealthy": true
}
```

### `POST /api/environments/{envId}/jetstream/streams/{stream}/consumers`

Create a consumer.

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "name": "order-processor",
  "durableName": "order-processor",
  "deliverPolicy": "All",
  "ackPolicy": "Explicit",
  "filterSubject": "orders.created"
}
```

**Response** `201 Created`.

### `PUT /api/environments/{envId}/jetstream/streams/{stream}/consumers/{name}`

Update consumer configuration.

**Authorization**: Operator or Administrator.

**Response** `200 OK`.

### `DELETE /api/environments/{envId}/jetstream/streams/{stream}/consumers/{name}`

Delete a consumer.

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true`.

**Response** `204 No Content`.

---

## 5. KV Store — `/api/environments/{envId}/kv`

### `GET /api/environments/{envId}/kv/buckets`

List KV buckets.

**Authorization**: Any authenticated user.

**Response** `200 OK`: Paginated list of bucket summaries.

### `GET /api/environments/{envId}/kv/buckets/{bucket}`

Get bucket details.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "bucketName": "config",
  "history": 5,
  "maxBytes": 1048576,
  "maxValueSize": 65536,
  "ttl": null,
  "keyCount": 42,
  "byteCount": 8192
}
```

### `POST /api/environments/{envId}/kv/buckets`

Create a KV bucket.

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "bucketName": "config",
  "history": 5,
  "maxBytes": 1048576,
  "maxValueSize": 65536,
  "ttl": null
}
```

**Response** `201 Created`.

### `DELETE /api/environments/{envId}/kv/buckets/{bucket}`

Delete a KV bucket.

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true`.

**Response** `204 No Content`.

### `GET /api/environments/{envId}/kv/buckets/{bucket}/keys`

List keys in a bucket.

**Authorization**: Any authenticated user.

**Query**: `?search=app.settings&page=1&pageSize=100`

**Response** `200 OK`:
```json
{
  "items": [
    {
      "key": "app.settings.theme",
      "revision": 3,
      "operation": "Put",
      "createdAt": "2026-04-06T10:00:00Z",
      "size": 128
    }
  ]
}
```

### `GET /api/environments/{envId}/kv/buckets/{bucket}/keys/{key}`

Get key value and metadata.

**Authorization**: Any authenticated user (payload access may be restricted).

**Response** `200 OK`:
```json
{
  "key": "app.settings.theme",
  "value": "base64-content",
  "revision": 3,
  "operation": "Put",
  "createdAt": "2026-04-06T10:00:00Z"
}
```

### `GET /api/environments/{envId}/kv/buckets/{bucket}/keys/{key}/history`

Get key revision history.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "entries": [
    { "revision": 3, "operation": "Put", "createdAt": "2026-04-06T10:00:00Z", "size": 128 },
    { "revision": 2, "operation": "Put", "createdAt": "2026-04-05T09:00:00Z", "size": 64 },
    { "revision": 1, "operation": "Put", "createdAt": "2026-04-04T08:00:00Z", "size": 32 }
  ]
}
```

### `PUT /api/environments/{envId}/kv/buckets/{bucket}/keys/{key}`

Create or update a key value.

**Authorization**: Operator or Administrator.

**Request**:
```json
{
  "value": "base64-content",
  "expectedRevision": 3
}
```

`expectedRevision` is optional. If provided, the operation fails with `409 Conflict` if the key was modified since that revision (optimistic concurrency).

**Response** `200 OK`:
```json
{
  "revision": 4
}
```

### `DELETE /api/environments/{envId}/kv/buckets/{bucket}/keys/{key}`

Delete a key.

**Authorization**: Operator or Administrator.

**Request header**: `X-Confirm: true`.

**Response** `204 No Content`.

---

## 6. Object Store — `/api/environments/{envId}/objects`

### `GET /api/environments/{envId}/objects/buckets`

List Object Store buckets.

**Authorization**: Any authenticated user.

**Response** `200 OK`: Paginated list of bucket summaries.

### `GET /api/environments/{envId}/objects/buckets/{bucket}`

Get bucket details.

**Authorization**: Any authenticated user.

### `POST /api/environments/{envId}/objects/buckets`

Create an Object Store bucket.

**Authorization**: Operator or Administrator.

### `DELETE /api/environments/{envId}/objects/buckets/{bucket}`

Delete an Object Store bucket.

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true`.

### `GET /api/environments/{envId}/objects/buckets/{bucket}/objects`

List objects in a bucket.

**Authorization**: Any authenticated user.

**Query**: `?search=report&page=1&pageSize=50`

**Response** `200 OK`:
```json
{
  "items": [
    {
      "name": "monthly-report.pdf",
      "description": "April 2026 report",
      "size": 1048576,
      "chunks": 4,
      "digest": "sha256:abc123...",
      "modifiedAt": "2026-04-06T10:00:00Z"
    }
  ]
}
```

### `GET /api/environments/{envId}/objects/buckets/{bucket}/objects/{name}`

Get object metadata.

**Authorization**: Any authenticated user.

### `GET /api/environments/{envId}/objects/buckets/{bucket}/objects/{name}/download`

Download object content.

**Authorization**: Operator or Administrator (payload access restricted by role).

**Response**: `200 OK` with `application/octet-stream` body, `Content-Disposition: attachment`.

### `PUT /api/environments/{envId}/objects/buckets/{bucket}/objects/{name}`

Upload or replace an object.

**Authorization**: Operator or Administrator.

**Request**: `multipart/form-data` with file upload + optional description.

**Request header**: `X-Confirm: true` (for replace).

**Response** `200 OK` or `201 Created`.

### `DELETE /api/environments/{envId}/objects/buckets/{bucket}/objects/{name}`

Delete an object.

**Authorization**: Administrator (or Operator in non-production).

**Request header**: `X-Confirm: true`.

**Response** `204 No Content`.

---

## 7. NATS Services — `/api/environments/{envId}/services`

### `GET /api/environments/{envId}/services`

Discover available services.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "items": [
    {
      "name": "order-service",
      "id": "instance-1",
      "version": "1.2.0",
      "description": "Order processing service",
      "isAvailable": true,
      "endpointCount": 3
    }
  ]
}
```

### `GET /api/environments/{envId}/services/{name}`

Get service details.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "name": "order-service",
  "id": "instance-1",
  "version": "1.2.0",
  "description": "Order processing service",
  "isAvailable": true,
  "endpoints": [
    {
      "name": "create-order",
      "subject": "orders.create",
      "queueGroup": "order-workers",
      "numRequests": 5420,
      "numErrors": 12,
      "averageProcessingTimeMs": 45.2
    }
  ]
}
```

### `POST /api/environments/{envId}/services/{name}/test`

Send a test request to a service endpoint.

**Authorization**: Administrator.

**Request**:
```json
{
  "endpointSubject": "orders.create",
  "payload": "base64-content",
  "headers": {},
  "timeoutSeconds": 5
}
```

**Response** `200 OK`:
```json
{
  "success": true,
  "responsePayload": "base64-content",
  "responseHeaders": {},
  "latencyMs": 42,
  "sideEffectWarningAcknowledged": true
}
```

---

## 8. Monitoring — `/api/environments/{envId}/monitoring`

### `GET /api/environments/{envId}/monitoring/dashboard`

Get environment health dashboard summary.

**Authorization**: Any authenticated user.

**Response** `200 OK`:
```json
{
  "environment": {
    "name": "production-us-east",
    "connectionStatus": "Available"
  },
  "coreNats": {
    "clientCount": 42,
    "subscriptionCount": 128
  },
  "jetstream": {
    "streamCount": 12,
    "consumerCount": 35,
    "unhealthyConsumers": 2,
    "totalMessages": 524000,
    "totalBytes": 104857600
  },
  "kv": {
    "bucketCount": 5,
    "totalKeys": 342
  },
  "objectStore": {
    "bucketCount": 2,
    "totalObjects": 28,
    "totalBytes": 52428800
  },
  "services": {
    "totalServices": 8,
    "availableServices": 7,
    "unavailableServices": 1
  },
  "alerts": [
    {
      "severity": "Warning",
      "resourceType": "Consumer",
      "resourceName": "order-processor",
      "message": "Consumer backlog growing: 1,542 pending messages",
      "detectedAt": "2026-04-06T11:45:00Z"
    }
  ]
}
```

---

## 9. Audit — `/api/audit`

### `GET /api/audit/events`

Search and filter audit events.

**Authorization**: Administrator or Auditor.

**Query**: `?actorId=guid&actionType=Delete&resourceType=Stream&environmentId=guid&source=UserInitiated&from=2026-04-01&to=2026-04-06&page=1&pageSize=50`

**Response** `200 OK`: Paginated list of audit events.
```json
{
  "items": [
    {
      "id": "guid",
      "timestamp": "2026-04-06T11:30:00Z",
      "actorName": "admin@company.com",
      "actionType": "Delete",
      "resourceType": "Stream",
      "resourceName": "OLD-ORDERS",
      "environmentId": "guid",
      "outcome": "Success",
      "source": "UserInitiated",
      "details": { "reason": "Stream no longer needed" }
    }
  ]
}
```

---

## 10. Access Control — `/api/access`

### `GET /api/access/users`

List users.

**Authorization**: Administrator.

### `POST /api/access/users`

Create a user.

**Authorization**: Administrator.

**Request**:
```json
{
  "username": "operator1",
  "displayName": "Jane Operator",
  "password": "secure-password",
  "roleAssignments": [
    { "roleId": "guid", "environmentId": null }
  ]
}
```

### `PUT /api/access/users/{id}`

Update user details.

**Authorization**: Administrator.

### `DELETE /api/access/users/{id}`

Deactivate a user.

**Authorization**: Administrator.

### `GET /api/access/roles`

List available roles.

**Authorization**: Administrator.

### `POST /api/access/users/{userId}/roles`

Assign a role to a user.

**Authorization**: Administrator.

**Request**:
```json
{
  "roleId": "guid",
  "environmentId": "guid-or-null"
}
```

### `DELETE /api/access/users/{userId}/roles/{assignmentId}`

Revoke a role assignment.

**Authorization**: Administrator.

---

## 11. Authentication — `/api/auth`

### `POST /api/auth/login`

Authenticate and create session.

**Request**:
```json
{
  "username": "operator1",
  "password": "secure-password"
}
```

**Response** `200 OK` + `Set-Cookie: session=...`:
```json
{
  "userId": "guid",
  "displayName": "Jane Operator",
  "roles": ["Operator"]
}
```

### `POST /api/auth/logout`

End session.

**Response** `204 No Content`.

### `GET /api/auth/me`

Get current user context.

**Response** `200 OK`:
```json
{
  "userId": "guid",
  "displayName": "Jane Operator",
  "roles": [
    { "role": "Operator", "environmentId": null }
  ],
  "permissions": {
    "canManageUsers": false,
    "canViewAudit": false,
    "canPerformDestructive": true,
    "productionRestricted": true
  }
}
```

---

## 12. Search — `/api/search`

### `GET /api/search`

Cross-resource search.

**Authorization**: Any authenticated user.

**Query**: `?q=orders&types=Stream,Consumer,KvBucket&environmentId=guid&page=1&pageSize=20`

**Response** `200 OK`:
```json
{
  "items": [
    {
      "resourceType": "Stream",
      "resourceId": "ORDERS",
      "resourceName": "ORDERS",
      "environmentId": "guid",
      "environmentName": "production-us-east",
      "summary": "15,420 messages, 3 consumers",
      "url": "/environments/guid/jetstream/streams/ORDERS"
    }
  ]
}
```

---

## 13. Bookmarks — `/api/bookmarks`

### `GET /api/bookmarks`

List user's bookmarks.

**Authorization**: Any authenticated user (own bookmarks only).

### `POST /api/bookmarks`

Create a bookmark.

**Request**:
```json
{
  "environmentId": "guid",
  "resourceType": "Stream",
  "resourceId": "ORDERS",
  "displayName": "Orders stream (prod)"
}
```

**Response** `201 Created`.

### `DELETE /api/bookmarks/{id}`

Remove a bookmark.

**Response** `204 No Content`.

---

## 14. Health — `/health`

### `GET /health`

Application health check (for Kubernetes probes).

**Authorization**: None (public).

**Response** `200 OK`:
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "natsConnections": "Degraded"
  }
}
```

---

## 15. User Preferences — `/api/preferences`

### `GET /api/preferences`

Get current user's preferences.

**Authorization**: Any authenticated user (own preferences only).

**Response** `200 OK`:
```json
{
  "items": [
    { "key": "defaultEnvironmentId", "value": "guid" },
    { "key": "theme", "value": "dark" },
    { "key": "listPageSize", "value": "50" }
  ]
}
```

### `PUT /api/preferences/{key}`

Set a user preference.

**Authorization**: Any authenticated user (own preferences only).

**Request**:
```json
{
  "value": "dark"
}
```

**Response** `200 OK`:
```json
{
  "key": "theme",
  "value": "dark"
}
```

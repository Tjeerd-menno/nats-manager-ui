# API Contracts: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Branch**: `002-core-nats-subjects-messaging`
**Date**: 2026-04-25

All endpoints are under `/api/environments/{envId:guid}/core-nats/`. Authentication is required for all. Publishing requires `OperatorAccess` policy.

---

## Existing Endpoints (unchanged response shape)

### `GET /status`
Returns `NatsServerInfo`. No change.

### `GET /clients`
Returns `NatsClientInfo[]`. No change.

---

## Updated: `GET /subjects`

Previously returned `[]`. Now returns subjects from the NATS HTTP monitoring API.

**Response: `200 OK`**
```json
[
  { "subject": "orders.created", "subscriptions": 3 },
  { "subject": "events.>",       "subscriptions": 1 }
]
```

**Response: `200 OK` (monitoring unavailable)**
```json
[]
```
An empty array is returned (not an error) when the NATS monitoring endpoint cannot be reached. The frontend distinguishes this from "zero subscriptions" by showing a static informational placeholder when no subjects are returned and monitoring is unavailable.

> **Note**: The frontend differentiates "monitoring unavailable" from "truly empty" using a companion field returned by the backend: a `X-Subjects-Source: monitoring | unavailable` response header (see research note on graceful degradation).

**Response header** (new):
```
X-Subjects-Source: monitoring        # subjects fetched from /subsz
X-Subjects-Source: unavailable       # monitoring endpoint unreachable; empty array returned
```

---

## Updated: `POST /publish`

**Request** (expanded body):
```json
{
  "subject": "orders.created",
  "payload": "{\"orderId\": \"abc123\"}",
  "payloadFormat": "Json",
  "headers": {
    "X-Source": "nats-admin-ui",
    "X-Correlation-Id": "req-456"
  },
  "replyTo": "orders.created.reply"
}
```

| Field | Type | Required | Default | Notes |
|-------|------|----------|---------|-------|
| `subject` | string | ✅ | — | Non-empty NATS subject |
| `payload` | string | ❌ | `null` | Encoded per `payloadFormat` |
| `payloadFormat` | `"PlainText" \| "Json" \| "HexBytes"` | ❌ | `"PlainText"` | Controls byte encoding |
| `headers` | `object` | ❌ | `{}` | String key → string value pairs; keys must not be empty or whitespace-only. The UI rejects duplicate keys, while duplicate JSON object keys received by the backend follow standard last-value-wins deserialization before validation. |
| `replyTo` | string | ❌ | `null` | Optional reply-to subject |

**Response: `200 OK`**
```json
{ "published": true }
```

**Response: `422 Unprocessable Entity`** (validation failure)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Subject": ["'Subject' must not be empty."],
    "Headers": ["Header key must not be empty."]
  }
}
```

**Response: `422 Unprocessable Entity`** (invalid hex or JSON when format demands valid encoding)
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.21",
  "title": "Validation failed",
  "status": 422,
  "errors": {
    "Payload": ["Payload is not valid hex-encoded bytes."]
  }
}
```

---

## New: `GET /stream` (Server-Sent Events)

Streams live messages from a NATS subscription to the browser.

**Request**:
```
GET /api/environments/{envId}/core-nats/stream?subject=orders.%3E
Accept: text/event-stream
```

| Query Parameter | Required | Description |
|-----------------|----------|-------------|
| `subject` | ✅ | NATS subject or wildcard pattern (URL-encoded) |

**Response headers**:
```
Content-Type: text/event-stream
Cache-Control: no-cache
X-Accel-Buffering: no
Connection: keep-alive
```

**SSE event format** (one per message):
```
event: message
data: {"subject":"orders.created","receivedAt":"2026-04-25T18:52:37.508Z","payloadBase64":"eyJvcmRlcklkIjoiYWJjMTIzIn0=","payloadSize":22,"headers":{"X-Source":"my-app"},"replyTo":null,"isBinary":false}

```

**SSE error event** (invalid subject pattern):
```
event: error
data: {"code":"INVALID_SUBJECT","message":"Subject pattern must not contain spaces."}

```

**Connection lifecycle**:
- Client opens `EventSource` → backend opens NATS subscription
- Client closes `EventSource` (navigate away, explicit close) → `HttpContext.RequestAborted` fires → NATS subscription disposed
- No explicit unsubscribe message needed (SSE is GET + server push only)

**Authorization**: Requires authenticated session (same as all other endpoints). Read-only access allowed (Operator and above). Anti-forgery is not enforced on GET endpoints per existing middleware configuration.

**Validation error: `400 Bad Request`** (subject empty or contains spaces — returned as HTTP error before SSE stream opens):
```json
{
  "status": 400,
  "title": "Bad Request",
  "detail": "Subject pattern must not be empty."
}
```

---

## Frontend Hook Contracts

### `useSubjects(environmentId)`
```typescript
interface UseSubjectsResult {
  data: NatsSubjectInfo[] | undefined;
  isLoading: boolean;
  error: Error | null;
  isMonitoringAvailable: boolean;
}

function useSubjects(environmentId: string | null): UseSubjectsResult
```
- Query key: `['core-nats-subjects', environmentId]`
- `refetchInterval: 15000`
- `enabled: !!environmentId`
- Reads `X-Subjects-Source` to expose `isMonitoringAvailable`.

### `usePublishMessage(environmentId)` (updated)
```typescript
function usePublishMessage(environmentId: string | null): UseMutationResult<void, Error, PublishRequest>
```
- `POST /environments/{envId}/core-nats/publish`
- On success: does **not** reset form (caller responsibility)

### `useLiveMessages(environmentId)` (new)
```typescript
interface UseLiveMessagesReturn {
  messages: NatsLiveMessage[];
  isConnected: boolean;
  isPaused: boolean;
  pendingCount: number;
  cap: number;
  setCap: (cap: number) => void;
  subscribe: (subject: string) => void;
  unsubscribe: () => void;
  pause: () => void;
  resume: () => void;
  clear: () => void;
}

function useLiveMessages(environmentId: string | null): UseLiveMessagesReturn
```

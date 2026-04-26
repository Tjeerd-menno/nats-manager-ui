# Data Model: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Branch**: `002-core-nats-subjects-messaging`
**Date**: 2026-04-25
**Source**: [spec.md](spec.md) key entities + [research.md](research.md) design decisions

---

## Persistence Boundary

This feature introduces **no new persisted entities**. All data flows through:
- **NATS HTTP monitoring API** (subject list — ephemeral, polled)
- **NATS.Net connection** (publish with headers; live subscription stream)
- **In-memory frontend state** (live message buffer — not persisted)

---

## Application Layer Models (DTOs)

### Updated: `NatsSubjectInfo`

Existing record, used unchanged from backend perspective. Frontend type corrected to match.

| Field | Type | Description |
|-------|------|-------------|
| `Subject` | `string` | NATS subject string (e.g. `orders.>`) |
| `Subscriptions` | `int` | Number of active subscriptions on this subject |

**Frontend fix**: `types.ts` `NatsSubjectInfo` currently uses `name`+`messageCount`; corrected to `subject`+`subscriptions`.

---

### New: `PayloadFormat` (Enum)

```csharp
public enum PayloadFormat { PlainText, Json, HexBytes }
```

Used in `PublishMessageCommand` to control how the string payload is encoded to bytes before sending.

---

### Updated: `PublishMessageCommand`

Extended with three new optional fields:

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Subject` | `string` | (required) | NATS subject to publish to |
| `Payload` | `string?` | `null` | String payload content |
| `PayloadFormat` | `PayloadFormat` | `PlainText` | How payload string is encoded to bytes |
| `Headers` | `IReadOnlyDictionary<string, string>` | `{}` | Custom NATS message headers |
| `ReplyTo` | `string?` | `null` | Optional reply-to subject |

**Encoding rules**:
- `PlainText` → `Encoding.UTF8.GetBytes(payload)`
- `Json` → validated with `JsonDocument.Parse`; if invalid → validation failure (`422`); encoded as UTF-8 bytes
- `HexBytes` → `Convert.FromHexString(payload)`; if invalid hex → validation failure (`422`)
- Header keys must not be empty or whitespace-only. The frontend rejects duplicate keys before submit; JSON object duplicate keys received by the backend follow standard JSON deserialization semantics where the last value wins before validation.

---

### New: `NatsLiveMessage`

Represents a single message received from a live NATS subscription.

| Field | Type | Description |
|-------|------|-------------|
| `Subject` | `string` | The subject the message arrived on |
| `ReceivedAt` | `DateTimeOffset` | Server-side timestamp when message was received |
| `PayloadBase64` | `string` | Base64-encoded raw payload bytes (client decides rendering) |
| `PayloadSize` | `int` | Byte count of payload |
| `Headers` | `IReadOnlyDictionary<string, string>` | Key→value headers (multi-value collapsed with `, `) |
| `ReplyTo` | `string?` | Reply-to subject if present |
| `IsBinary` | `bool` | `true` if payload bytes are not valid UTF-8 |

**Why Base64?** SSE events are text; raw binary cannot be embedded. The frontend decodes Base64 and renders as UTF-8 text or hex string based on `IsBinary`.

---

### Updated: `PublishMessageBody` (Web Layer Request DTO)

```csharp
public sealed record PublishMessageBody(
    string Subject,
    string? Payload,
    PayloadFormat PayloadFormat = PayloadFormat.PlainText,
    Dictionary<string, string>? Headers = null,
    string? ReplyTo = null);
```

---

## Port Interface Changes

### Updated: `ICoreNatsAdapter`

```csharp
public interface ICoreNatsAdapter
{
    Task<NatsServerInfo?> GetServerInfoAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<ListSubjectsResult> ListSubjectsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NatsClientInfo>> ListClientsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid environmentId, string subject, byte[] data,
        IReadOnlyDictionary<string, string>? headers = null,
        string? replyTo = null,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<NatsLiveMessage> SubscribeAsync(Guid environmentId, string subject, CancellationToken cancellationToken = default);
}
```

---

## Infrastructure: HTTP Monitoring Options

A new options class for configurable monitoring port:

```csharp
public sealed class CoreNatsMonitoringOptions
{
    public const string SectionName = "CoreNats:Monitoring";
    public int DefaultPort { get; set; } = 8222;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
```

Registered via `IOptions<CoreNatsMonitoringOptions>` in DI.

---

## Frontend Type Changes

### Updated `NatsSubjectInfo`
```typescript
export interface NatsSubjectInfo {
  subject: string;        // was: name
  subscriptions: number;  // was: messageCount
}
```

### New: `PayloadFormat`
```typescript
export type PayloadFormat = 'PlainText' | 'Json' | 'HexBytes';
```

### New: `PublishRequest`
```typescript
export interface PublishRequest {
  subject: string;
  payload?: string;
  payloadFormat: PayloadFormat;
  headers: Record<string, string>;
  replyTo?: string;
}
```

### New: `NatsLiveMessage`
```typescript
export interface NatsLiveMessage {
  subject: string;
  receivedAt: string;      // ISO 8601
  payloadBase64: string;
  payloadSize: number;
  headers: Record<string, string>;
  replyTo?: string;
  isBinary: boolean;
}
```

---

## Frontend State: Live Message Viewer

Managed entirely in the `useLiveMessages` hook — no global store needed.

| State | Type | Description |
|-------|------|-------------|
| `messages` | `NatsLiveMessage[]` | Currently displayed messages (max `cap`) |
| `isConnected` | `boolean` | Whether `EventSource` is open |
| `isPaused` | `boolean` | Whether display updates are paused |
| `pendingCount` | `number` | Count of buffered messages while paused |
| `cap` | `number` | User-configured max messages (100–500) |
| `activeSubject` | `string \| null` | Currently subscribed subject pattern |

The hook stores a separate `pendingBuffer` ref for messages received while paused. On `resume()`, pending messages are flushed into `messages` (respecting cap) and `pendingBuffer` is cleared.

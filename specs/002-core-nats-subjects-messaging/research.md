# Research: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Branch**: `002-core-nats-subjects-messaging`
**Date**: 2026-04-25
**Purpose**: Consolidate technology decisions, design rationale, and constraints discovered during codebase analysis.

---

## 1. Current State of Core NATS

### What exists

| Area | Current State |
|------|--------------|
| `ICoreNatsAdapter` | Defines `ListSubjectsAsync`, `PublishAsync`; both partially implemented |
| `ListSubjectsAsync` | Returns empty list — stub with comment "can be enhanced with NATS monitoring API" |
| `PublishAsync` | Functional but accepts raw `byte[]`; no headers, no reply-to |
| `GetSubjectsQuery/Handler` | Wired up and tested; calls the stub |
| `CoreNatsEndpoints` | `GET /subjects` returns `[]`; `POST /publish` accepts `{subject, payload}` |
| `CoreNatsPage.tsx` | Server info cards + bare publish modal (subject + payload only) |
| `useCoreNats.ts` | `useCoreNatsStatus` (polling) + `usePublishMessage` (mutation); no subjects hook |
| Frontend `types.ts` | `NatsSubjectInfo` has `name`+`messageCount` — mismatched with backend `Subject`+`Subscriptions` |

### Key gaps

1. Subject discovery: needs real implementation via NATS HTTP monitoring `/subsz` endpoint
2. Publish: no headers, no reply-to, no payload format control
3. Message viewer: not started anywhere in the stack

---

## 2. Subject Discovery via NATS HTTP Monitoring

### Option A: NATS $SYS subjects (system account)
Use `$SYS.REQ.SERVER.STATSZ` or `$SYS.REQ.ACCOUNT.STATZ` to get per-subject stats.  
**Rejected** — requires system account credentials not available in all deployments; same limitation hit for `$SYS.REQ.SERVER.PING` (already gracefully degraded in `GetServerInfoAsync`).

### Option B: NATS HTTP monitoring endpoint `/subsz`
Call `http://<host>:<monitoring-port>/subsz?subs=1` to get a full subscription list.  
**Selected** — works on any NATS server with the monitoring port open; well-documented REST API; returns per-subject subscription counts.

**Monitoring port derivation**: NATS default monitoring port is `8222`. The `Environment.ServerUrl` is a NATS client URL like `nats://host:4222`. We extract the host and attempt `http://host:8222/subsz`. If the HTTP call fails (connection refused, 4xx, timeout), `ListSubjectsAsync` returns an empty list — the same graceful degradation already in place.

**`/subsz` response shape** (relevant subset):
```json
{
  "num_subscriptions": 42,
  "num_cache": 5,
  "num_inserts": 100,
  "subslist": [
    { "subject": "orders.>", "queueName": "", "num_msgs": 0 }
  ]
}
```
Each entry in `subslist` represents one unique subject+queue combination. We group by subject and count entries to derive the subscription count.

### Configurable monitoring port
The `Environment` domain model currently has no monitoring port field. Rather than adding a new field to the domain model (scope creep), we will:
1. Try host-derived port `8222` as default
2. Expose an `IOptions<CoreNatsMonitoringOptions>` with a configurable default port, allowing `appsettings.json` override
3. Document the assumption in comments and quickstart

---

## 3. Expanded Publish: Headers and Reply-To

### NATS.Net v2 API for headers
`NatsConnection.PublishAsync` accepts `NatsHeaders` (a `Dictionary<string, string[]>` derivative).
```csharp
var headers = new NatsHeaders();
headers["X-My-Header"] = new[] { "value" };
await connection.PublishAsync(subject, data, headers: headers, replyTo: replyToSubject);
```
This is already used in `ObjectStoreAdapter` for `Content-Type` headers — the pattern is established.

### Payload format encoding
| Format | Encoding | Server sends |
|--------|----------|-------------|
| Plain text | UTF-8 bytes | Raw string bytes |
| JSON | UTF-8 bytes (validated before encode) | Same as text — NATS has no JSON awareness |
| Hex bytes | Hex-decode to raw bytes | Binary bytes |

Hex decoding: `Convert.FromHexString(hexString)` (.NET 5+).

### Command expansion
`PublishMessageCommand` gains:
- `PayloadFormat` (enum: `PlainText`, `Json`, `HexBytes`)
- `Headers` (`IReadOnlyDictionary<string, string>`)
- `ReplyTo` (optional string)

The handler converts payload according to format, constructs `NatsHeaders`, and calls the updated adapter method.

### Frontend validation
JSON validation is purely client-side before submission. The backend does not re-validate JSON structure — it just encodes the string to bytes. This is correct: the backend is a transport layer, not a JSON parser.

---

## 4. Live Message Viewer: Transport Decision

### Option A: Short polling (GET /messages every N seconds)
Subscribe on each poll, drain messages, return batch.  
**Rejected** — not truly real-time; high latency between messages; wasteful NATS subscriptions opened/closed repeatedly.

### Option B: WebSocket
Bidirectional; client sends subscribe/unsubscribe commands; server pushes messages.  
**Considered** — more complex; requires ASP.NET Core WebSocket middleware; no existing WebSocket usage in codebase; bidirectionality not strictly needed.

### Option C: Server-Sent Events (SSE)
One-directional server→client stream; standard HTTP; works with existing `HttpClient` and `EventSource` browser API; `CancellationToken` automatically fires when client disconnects.  
**Selected** — simplest integration with Minimal APIs; `Results.Stream` or manual `text/event-stream` response; client disconnect cancels the NATS subscription automatically via `HttpContext.RequestAborted`.

### SSE endpoint design
```
GET /api/environments/{envId}/core-nats/stream?subject={pattern}
Accept: text/event-stream
```
- No authentication bypass — same session auth as other endpoints
- Backend creates a NATS subscription using `connection.SubscribeAsync<byte[]>(subject)`
- Each received message is serialised as JSON and written as an SSE `data:` event
- The endpoint loops until `cancellationToken` (wired to `HttpContext.RequestAborted`) fires
- Anti-forgery is exempted for SSE (GET + `EventSource` cannot send XSRF token; GET is read-only so this is safe)

### Frontend SSE hook
`useLiveMessages` hook:
- Manages `EventSource` lifecycle (open on subscribe, close on unsubscribe or component unmount)
- Maintains a bounded circular buffer (`useRef` + state) capped at user-configured limit
- Exposes `messages`, `isConnected`, `isPaused`, `pause()`, `resume()`, `clear()`, `subscribe(pattern)`, `unsubscribe()`
- When paused: collects messages in a separate buffer; flushes to display list on resume

### Memory safety
`EventSource` is created in a `useEffect` with cleanup. The cap enforced client-side prevents unbounded array growth. The `NatsSubscription` is cancelled when the HTTP request is cancelled (client disconnect).

---

## 5. Frontend Type Misalignment Fix

`types.ts` `NatsSubjectInfo` uses `name` + `messageCount` but the backend model uses `subject` + `subscriptions`. The fix aligns the frontend type to match the API response shape actually returned by the query handler.

---

## 6. Existing Test Coverage Impact

All new backend functionality adds to existing test files following established patterns:

| File | Change |
|------|--------|
| `CoreNatsQueryCommandTests.cs` | Add tests for `PublishMessageCommand` with headers/replyTo/format; new `SubscribeToSubjectQuery` tests |
| `CoreNatsEndpointTests.cs` | Add tests for SSE subscribe endpoint; updated publish body |
| `CoreNatsAdapterTests.cs` (integration) | Add tests for `ListSubjectsAsync` (monitoring HTTP) and `SubscribeAsync` |
| `CoreNatsTests.cs` (E2E) | Extend with subjects list, expanded publish (headers), live viewer API test |
| `CoreNatsPage.test.tsx` | Extend for subject browser section, new publish form fields |
| New `hooks/useCoreNats.test.ts` additions | `useSubjects`, `useLiveMessages` hook tests |
| New `SubjectBrowser.test.tsx` | Component test |
| New `LiveMessageViewer.test.tsx` | Component test |

---

## 7. Security Considerations

- **SSE endpoint (`GET .../stream`)**: Read-only; uses same session auth; no XSRF token needed for GET. Anti-forgery middleware is scoped to non-GET mutating requests — SSE is safe.
- **Header injection**: User-supplied header keys and values are passed verbatim to `NatsHeaders`. NATS headers have no injection risk at the NATS protocol level; however, the backend should validate that header keys are non-empty strings (added to `PublishMessageCommandValidator`).
- **Hex payload**: `Convert.FromHexString` throws `FormatException` on invalid hex; caught and returned as a `400 Bad Request` via the existing validation pipeline.
- **Subject pattern wildcards**: `*` and `>` in subscribe patterns are valid NATS wildcards; no sanitisation needed beyond ensuring the string is non-empty and contains no spaces.

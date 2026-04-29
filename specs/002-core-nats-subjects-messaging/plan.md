# Implementation Plan: Core NATS Subjects Browser, Expanded Publishing & Message Viewer

**Branch**: `002-core-nats-subjects-messaging` | **Date**: 2026-04-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-core-nats-subjects-messaging/spec.md`

## Summary

Rework the Core NATS page in three independently deliverable increments:

1. **Subject Browser (P1)**: Implement the stub `ListSubjectsAsync` via the NATS HTTP monitoring `/subsz` endpoint, surface the result in a filterable table on `CoreNatsPage`.
2. **Expanded Publish (P2)**: Extend `PublishMessageCommand` and the UI publish form with payload format selection (Plain Text / JSON / Hex Bytes), arbitrary message headers (key–value rows), and an optional reply-to subject.
3. **Live Message Viewer (P3)**: Add a new SSE endpoint `GET /stream?subject=` that bridges a NATS subscription to the browser via Server-Sent Events, backed by a `useLiveMessages` hook and `LiveMessageViewer` component.

See [research.md](research.md), [data-model.md](data-model.md), [contracts/api-contracts.md](contracts/api-contracts.md), and [quickstart.md](quickstart.md) for supporting detail.

---

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript strict mode (frontend)  
**Primary Dependencies**: ASP.NET Core 10 (Minimal APIs), NATS.Net v2, FluentValidation, React 19, Mantine 9, TanStack Query, Vite  
**Storage**: No new persistence — all data is ephemeral (NATS HTTP monitoring, NATS subscriptions, in-memory frontend buffer)  
**Testing**: xUnit v3 + MTP v2, Shouldly, NSubstitute (backend); Vitest + React Testing Library (frontend); Playwright (E2E)  
**Target Platform**: Desktop browsers; Linux backend  
**Project Type**: Web application (SPA + Minimal API)  
**Performance Goals**: Subject list visible < 2s; filter < 300ms; live message latency < 2s; viewer responsive at 50 msg/s  
**Constraints**: SSE subscription closed on navigate; message cap 100–500; monitoring port unavailability must not break page load  
**Scale/Scope**: Three user stories; ~15 backend files touched/added; ~10 frontend files touched/added; no schema migration

---

## Constitution Check

### I. Code Quality — ✅ PASS

- All new classes have single responsibility (monitoring HTTP client, SSE writer, payload format encoder are separate concerns)
- No functions exceed ~40 lines; SSE streaming loop is decomposed
- NATS domain terminology used throughout (subjects, subscriptions, reply-to — not "topics" or "channels")
- No `any` types in frontend; strict TypeScript throughout
- No code duplication — `PayloadViewer` shared component reused in `LiveMessageViewer`

### II. Testing Standards — ✅ PASS

- Unit tests added for all new command/query handlers and validators
- Integration tests added for `ListSubjectsAsync` (real NATS) and `SubscribeAsync`
- Frontend hook tests for `useSubjects` and `useLiveMessages`
- Component tests for `SubjectBrowser` and `LiveMessageViewer`
- E2E tests extended for all three user stories via Playwright
- 80% new-code coverage target maintained

### III. UX Consistency — ✅ PASS

- Subject browser follows same table/filter pattern as JetStream stream list
- Empty and unavailable states explicitly handled (not blank screens)
- Publish form success notification consistent with other mutating forms
- Live viewer pause/resume and cap controls follow same visual language as other controls
- `PayloadViewer` shared component reused for consistent payload rendering

### IV. Performance — ✅ PASS

- Subject list uses same 15-second polling interval as server status (no new polling cycle)
- Message cap (default 100) prevents unbounded memory growth
- SSE stream uses `IAsyncEnumerable` — no thread blocking
- `EventSource` closed on component unmount — no memory leaks

---

## Project Structure

### Documentation (this feature)

```text
specs/002-core-nats-subjects-messaging/
├── plan.md                        # This file
├── research.md                    # Technology decisions and rationale
├── data-model.md                  # Model changes and type definitions
├── quickstart.md                  # Developer setup and verification steps
├── spec.md                        # Feature specification
├── checklists/
│   └── requirements.md            # Quality checklist (all pass)
└── contracts/
    └── api-contracts.md           # API request/response contracts
```

### Source Code — files changed or added

```text
src/
├── NatsManager.Application/
│   └── Modules/CoreNats/
│       ├── Commands/
│       │   └── CoreNatsCommands.cs         ← Extend PublishMessageCommand (headers, replyTo, format)
│       ├── Models/
│       │   └── CoreNatsModels.cs           ← Add NatsLiveMessage, PayloadFormat enum
│       ├── Ports/
│       │   └── ICoreNatsAdapter.cs         ← Update PublishAsync signature; add SubscribeAsync
│       └── Queries/
│           └── CoreNatsQueries.cs          ← (no change needed; queries delegate to adapter)
│
├── NatsManager.Infrastructure/
│   ├── Configuration/
│   │   └── CoreNatsMonitoringOptions.cs   ← NEW: options for monitoring port + HTTP timeout
│   └── Nats/
│       └── CoreNatsAdapter.cs             ← Implement ListSubjectsAsync; update PublishAsync; add SubscribeAsync
│
└── NatsManager.Web/
    └── Endpoints/
        └── CoreNatsEndpoints.cs           ← Update publish body; add SSE stream endpoint; register options
```

```text
src/NatsManager.Frontend/src/features/corenats/
├── CoreNatsPage.tsx                        ← Rework page layout to compose new sections
├── types.ts                                ← Fix NatsSubjectInfo; add NatsLiveMessage, PublishRequest, PayloadFormat
├── hooks/
│   └── useCoreNats.ts                      ← Add useSubjects, useLiveMessages; update usePublishMessage
└── components/
    ├── SubjectBrowser.tsx                  ← NEW: filterable subject table
    ├── SubjectBrowser.test.tsx             ← NEW
    ├── PublishMessageForm.tsx              ← NEW: extracted + expanded publish form
    ├── PublishMessageForm.test.tsx         ← NEW
    ├── LiveMessageViewer.tsx               ← NEW: SSE-backed message feed
    └── LiveMessageViewer.test.tsx          ← NEW
```

```text
tests/
├── NatsManager.Application.Tests/Modules/CoreNats/
│   └── CoreNatsQueryCommandTests.cs        ← Add publish-with-headers, hex, JSON format tests
├── NatsManager.Integration.Tests/Nats/
│   └── CoreNatsAdapterTests.cs             ← Add ListSubjectsAsync (real NATS) and SubscribeAsync tests
├── NatsManager.Web.Tests/Endpoints/
│   └── CoreNatsEndpointTests.cs            ← Add SSE endpoint test; updated publish body tests
└── NatsManager.E2E.Tests/Tests/
    └── CoreNatsTests.cs                    ← Extend with subjects table, expanded publish, live viewer
```

**Structure Decision**: Web application (existing Option 2 layout). No new projects; all changes are additive within the existing `CoreNats` module boundary.

---

## Implementation Phases

### Phase 0 — Research ✅ (complete)

- [x] Analysed existing `CoreNatsAdapter`, `CoreNatsEndpoints`, `CoreNatsPage`, hooks, tests
- [x] Confirmed NATS.Net `PublishAsync` supports `NatsHeaders` and `replyTo` (established pattern in `ObjectStoreAdapter`)
- [x] Confirmed NATS HTTP monitoring `/subsz` endpoint provides per-subject subscription counts
- [x] Selected SSE over WebSocket and polling for live viewer (see [research.md](research.md) §4)
- [x] Identified frontend type misalignment (`NatsSubjectInfo`)
- [x] Verified `IAsyncEnumerable` streaming is supported by Minimal API response pattern

---

### Phase 1 — Backend: Subject Browser (P1)

**Goal**: `GET /subjects` returns real data from NATS monitoring; gracefully returns `[]` + `X-Subjects-Source: unavailable` when monitoring is unreachable.

#### Steps

1. **Add `CoreNatsMonitoringOptions`** (`NatsManager.Infrastructure/Configuration/`)
   - Properties: `DefaultPort` (int, default `8222`), `HttpTimeout` (TimeSpan, default `3s`)
   - Register in `Program.cs` via `Configure<CoreNatsMonitoringOptions>`

2. **Implement `ListSubjectsAsync` in `CoreNatsAdapter`**
   - Inject `IOptions<CoreNatsMonitoringOptions>` and `IHttpClientFactory`
   - Extract host from `Environment.ServerUrl` (parse as URI, take `Host`)
   - Call `http://{host}:{monitoringPort}/subsz?subs=1` with timeout from options
   - Parse `subslist` array → group by subject → return `NatsSubjectInfo[]`
   - On any exception (connection refused, timeout, parse error) → log warning, return `[]`

3. **Add `X-Subjects-Source` response header in `CoreNatsEndpoints.GetSubjects`**
   - Return `monitoring` when adapter returns data, `unavailable` when empty due to monitoring failure
   - Since the adapter currently has no way to distinguish "empty because no subscriptions" from "empty because unavailable", add a `ListSubjectsResult` wrapper with a `IsMonitoringAvailable` flag

4. **Unit test** (`CoreNatsAdapterTests` and `CoreNatsQueryCommandTests`)
   - Test with mock HTTP returning valid `/subsz` JSON
   - Test fallback when HTTP throws

---

### Phase 2 — Frontend: Subject Browser (P1)

**Goal**: A filterable `SubjectBrowser` section appears on `CoreNatsPage` below server info cards.

#### Steps

1. **Fix `NatsSubjectInfo` type** in `types.ts` (`name`→`subject`, `messageCount`→`subscriptions`)

2. **Add `useSubjects` hook** in `useCoreNats.ts`
   - Same pattern as `useCoreNatsStatus`: `refetchInterval: 15000`
   - Returns `{ data, isLoading, error }` plus response header `isMonitoringAvailable` (read from custom hook or derived from empty array heuristic)

3. **Create `SubjectBrowser` component**
   - Props: `environmentId: string`
   - Uses `useSubjects`
   - `TextInput` for filter (client-side, debounced 300ms)
   - `Table` with columns: Subject, Subscriptions
   - Empty state: "No active subscriptions found" (when monitoring available + 0 results)
   - Unavailable state: "Subject discovery unavailable — monitoring endpoint not reachable" (informational `Alert`, not error)
   - Loading state: `LoadingState` shared component

4. **Integrate into `CoreNatsPage`**
   - Below server info `SimpleGrid`, add `<SubjectBrowser environmentId={...} />`

5. **Tests** (`SubjectBrowser.test.tsx`)
   - Renders subjects table
   - Filter reduces rows
   - Shows unavailable placeholder when header indicates unavailable

---

### Phase 3 — Backend: Expanded Publish (P2)

**Goal**: `POST /publish` accepts headers, reply-to, and payload format; handler encodes accordingly; audit trail records the publish.

#### Steps

1. **Add `PayloadFormat` enum** to `CoreNatsModels.cs`

2. **Update `PublishMessageCommand`** (`CoreNatsCommands.cs`)
   - Add `PayloadFormat PayloadFormat { get; init; }` (default `PlainText`)
   - Add `IReadOnlyDictionary<string, string> Headers { get; init; }` (default empty)
   - Add `string? ReplyTo { get; init; }`

3. **Update `PublishMessageCommandValidator`**
   - Add rule: each header key must not be empty string
   - Add rule: if `PayloadFormat == Json` and `Payload != null`, validate with `JsonDocument.Parse`; throw validation error if invalid
   - Add rule: if `PayloadFormat == HexBytes` and `Payload != null`, validate hex string format

4. **Update `PublishMessageCommandHandler`**
   - Encode payload based on `PayloadFormat` (UTF-8, hex-decode, or UTF-8 with pre-validated JSON)
   - Pass `Headers` and `ReplyTo` to adapter

5. **Update `ICoreNatsAdapter.PublishAsync` signature**
   - Add `IReadOnlyDictionary<string, string>? headers = null, string? replyTo = null`

6. **Update `CoreNatsAdapter.PublishAsync`**
   - Build `NatsHeaders` from dictionary if non-empty
   - Pass `replyTo` to `connection.PublishAsync`

7. **Update `PublishMessageBody`** in `CoreNatsEndpoints.cs`
   - Add `PayloadFormat`, `Headers`, `ReplyTo` fields
   - Map to updated command

8. **Tests** (`CoreNatsQueryCommandTests.cs`, `CoreNatsEndpointTests.cs`)
   - Publish with headers → adapter receives correct headers
   - Publish with `Json` format + invalid JSON → validator fails
   - Publish with `HexBytes` format + invalid hex → validator fails
   - Publish with `ReplyTo` → adapter receives reply-to

---

### Phase 4 — Frontend: Expanded Publish Form (P2)

**Goal**: The publish modal becomes a feature-complete form with format selector, header rows, and reply-to field.

#### Steps

1. **Update `types.ts`** — add `PayloadFormat`, `PublishRequest`

2. **Update `usePublishMessage`** to accept `PublishRequest` (instead of `{subject, payload}`)

3. **Create `PublishMessageForm` component** (extracted from modal in `CoreNatsPage`)
   - `SegmentedControl` for payload format (Plain Text / JSON / Hex Bytes)
   - `Textarea` for payload with inline JSON validation error when format = JSON
   - Dynamic header rows: each row has a `TextInput` for key, `TextInput` for value, delete button; "Add Header" button appends a row
   - `TextInput` for reply-to (optional)
   - `Button` disabled while `isPending` or JSON is invalid
   - On success: show green `Notification` (do not clear form automatically)
   - On error: show red `Notification` (preserve all fields)

4. **Update `CoreNatsPage`** to use `<PublishMessageForm>` inside the modal

5. **Tests** (`PublishMessageForm.test.tsx`)
   - JSON format + invalid JSON → button disabled
   - Empty header key → validation error
   - Success notification rendered after successful mutate
   - Error notification rendered on mutation error, fields preserved

---

### Phase 5 — Backend: Live Message Viewer (P3)

**Goal**: SSE endpoint streams live NATS messages to the browser; subscription is torn down on client disconnect.

#### Steps

1. **Add `NatsLiveMessage`** to `CoreNatsModels.cs`

2. **Add `SubscribeAsync` to `ICoreNatsAdapter`**
   ```csharp
   IAsyncEnumerable<NatsLiveMessage> SubscribeAsync(Guid environmentId, string subject, CancellationToken cancellationToken = default);
   ```

3. **Implement `SubscribeAsync` in `CoreNatsAdapter`**
   - Get connection from factory
   - Call `connection.SubscribeAsync<byte[]>(subject, cancellationToken: cancellationToken)`
   - For each `NatsMsg<byte[]>` received: construct `NatsLiveMessage`
     - `Subject` from `msg.Subject`
     - `ReceivedAt` = `DateTimeOffset.UtcNow`
     - `PayloadBase64` = `Convert.ToBase64String(msg.Data ?? [])`
     - `PayloadSize` = `msg.Data?.Length ?? 0`
     - `Headers` = flatten `msg.Headers` to `Dictionary<string, string>` (join multi-value with `, `)
     - `ReplyTo` = `msg.ReplyTo`
     - `IsBinary` = test `Encoding.UTF8.GetString(bytes)` validity (catch `DecoderFallbackException`)
   - Yield each message; loop exits when `cancellationToken` fires

4. **Add `GET /stream` endpoint** in `CoreNatsEndpoints`
   - Read `subject` query parameter; validate non-empty and no spaces → `400` if invalid
   - Set response headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`
   - Iterate `SubscribeAsync`, serialize each message as JSON, write `data: {json}\n\n`, flush
   - No `RequireAuthorization(OperatorAccess)` needed — read-only; standard auth applies

5. **Tests** (`CoreNatsAdapterTests.cs`, `CoreNatsEndpointTests.cs`)
   - SSE endpoint returns `text/event-stream` content type
   - SSE endpoint returns `400` for empty subject
   - `SubscribeAsync` yields messages from mock connection

---

### Phase 6 — Frontend: Live Message Viewer (P3)

**Goal**: `LiveMessageViewer` component subscribes to a subject pattern via SSE, renders a bounded live log with pause/resume.

#### Steps

1. **Add `NatsLiveMessage` type** to `types.ts`

2. **Create `useLiveMessages` hook** in `useCoreNats.ts`
   - Manages `EventSource` via `useEffect`
   - State: `messages[]`, `isConnected`, `isPaused`, `pendingCount`, `cap`
   - `subscribe(subject)`: creates `EventSource` at `/api/environments/{envId}/corenats/stream?subject={encoded}`
   - `unsubscribe()`: closes `EventSource`, resets state
   - On each SSE `message` event: if paused → push to `pendingBuffer` ref, increment `pendingCount`; else → prepend to `messages` (keeping last `cap`)
   - `pause()` / `resume()`: toggle pause; on resume flush pending buffer into messages
   - `clear()`: reset messages + pendingBuffer
   - `setCap(n)`: update cap (clamp 100–500); trim messages if needed
   - Cleanup on unmount: close `EventSource`

3. **Create `LiveMessageViewer` component**
   - Subject pattern input + Subscribe/Unsubscribe buttons + connection status badge
   - Cap `NumberInput` (100–500)
   - Pause/Resume button + pending count badge
   - Clear button
   - `Table` of messages (most recent first):
     - Columns: Subject, Time, Payload Preview (first 80 chars), Headers count
     - Expandable rows: full payload via `PayloadViewer` shared component + header key/value list
   - Empty state when no messages yet
   - Pattern validation: warn inline if subject contains spaces

4. **Integrate into `CoreNatsPage`**
   - Add `<LiveMessageViewer environmentId={...} />` as a new section below subjects

5. **Tests** (`LiveMessageViewer.test.tsx`, `useCoreNats.test.ts` additions)
   - Subscribe button triggers `EventSource` open
   - Messages render in table on SSE event
   - Pause stops display; resume flushes pending
   - Cap trims oldest messages
   - Unsubscribe closes `EventSource`

---

### Phase 7 — E2E Tests (all stories)

Extend `CoreNatsTests.cs`:

1. Subject table visible and has at least one row when NATS has active subscribers
2. Filter reduces visible subject rows
3. Publish with JSON payload and custom header succeeds via UI
4. Live viewer receives a message published after subscribing

---

### Phase 8 — Final Validation

- Run full backend test suite: `dotnet test`
- Run frontend tests: `npm test` in `NatsManager.Frontend`
- Run E2E tests: `dotnet test tests/NatsManager.E2E.Tests`
- Verify lint: `dotnet format --verify-no-changes`, `npm run lint`

---

## Complexity Tracking

No constitution violations. All changes are additive within the existing `CoreNats` module. No new projects, no new databases, no new authentication mechanisms.

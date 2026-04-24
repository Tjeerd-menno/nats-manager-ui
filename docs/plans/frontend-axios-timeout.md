# F3 — Configure a request timeout on the shared axios client

## Context

`src/NatsManager.Frontend/src/api/client.ts` creates the single
`apiClient` axios instance used by every feature hook. It does not set a
`timeout`, so a stalled backend, a hanging NATS connection, or an
unreachable environment can leave a pending request indefinitely — the UI
shows a spinner forever and nothing frees the TanStack Query state.

## Goal

Every request goes through a sensible default timeout, with an escape hatch
for a small number of legitimately long-running endpoints (object-store
upload, stream-message fetch with a large count).

## Scope

### In scope

1. Set a default `timeout` on `apiClient` (suggest 30 000 ms; confirm the
   server-side `RequestTimeout` / Kestrel defaults first).
2. For calls that can legitimately exceed it — audit each and override the
   `timeout` at the call site, e.g.:
   - `POST /object-store/.../upload` (large blob uploads).
   - `GET /jetstream/.../messages` with large `count`.
   - Anything that streams or waits on a NATS request-reply with a
     configurable deadline.
3. Translate an axios timeout (`error.code === 'ECONNABORTED'`) into a
   user-visible notification via the helper introduced in F2
   ("The server took too long to respond.").
4. Document the default in a short JSDoc comment above `apiClient` so
   future contributors see it.

### Out of scope

- Do not introduce retry logic here — that's TanStack Query's job and is
  already configured in `src/NatsManager.Frontend/src/api/queryClient.ts`.
- Do not change the backend.

## Files expected to change

- `src/NatsManager.Frontend/src/api/client.ts`
- Individual feature hooks that issue the long-running requests listed
  above (per-call `timeout` override).
- `src/NatsManager.Frontend/src/shared/notifications.ts` (timeout-specific
  branch) — only if F2 has already landed; otherwise inline the check.

## Acceptance criteria

- [ ] `apiClient.defaults.timeout` is a non-zero number.
- [ ] Long-running endpoints explicitly set a higher `timeout` at the call
      site, with a brief code comment explaining why.
- [ ] `npm test` passes. Add a test that mocks axios to reject with
      `ECONNABORTED` and verifies the user-facing notification text.
- [ ] Manual smoke: hit a backend endpoint, kill the backend mid-request,
      confirm the spinner eventually stops and a "took too long" error
      appears.

## Risks / notes

- Choose the default in coordination with the server's per-endpoint
  timeouts. 30 s is a reasonable opening bid; revisit if it causes
  false positives on the dashboard query.
- Some endpoints take an `AbortSignal` via TanStack Query; make sure the
  axios `timeout` doesn't fight with an explicit `signal`.

## Reference instructions

- `.github/instructions/frontend.instructions.md`

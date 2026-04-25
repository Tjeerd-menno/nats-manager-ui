# F2 — Consistent `onError` handling for every TanStack Query mutation

## Context

Every mutation under `src/NatsManager.Frontend/src/features/**/hooks/use*.ts`
currently defines `onSuccess` (to invalidate queries or show a success
notification) but **not** `onError`. If the request fails, the error bubbles
up to the component's `mutation.mutate()` callsite where, in most features,
it's not caught at all. The user sees no feedback — the button just appears
to do nothing.

The shared Mantine notification helpers live at
`src/NatsManager.Frontend/src/shared/notifications.ts` and are already used
on success paths.

`src/NatsManager.Frontend/src/api/client.ts` exposes
`extractProblemDetails(error)` which returns an RFC 7807 `ProblemDetails`
object or `null`. This should be the preferred source of the error message.

## Goal

Every mutation surfaces failures to the user via a red notification with a
meaningful message, without changing the success paths.

## Scope

### In scope

- Add `onError` to every mutation in:
  - `features/jetstream/hooks/useJetStream.ts`
  - `features/kv/hooks/useKv.ts`
  - `features/objectstore/hooks/useObjectStore.ts`
  - `features/admin/hooks/useAdmin.ts`
  - `features/environments/hooks/useEnvironments.ts`
  - `features/search/hooks/useSearch.ts`
  - any other `useMutation` in `features/**` found by
    `rg "useMutation\(" src/NatsManager.Frontend/src/features`.
- Introduce a shared helper (e.g. in
  `src/NatsManager.Frontend/src/shared/notifications.ts`) such as
  `notifyMutationError(error, fallbackMessage)` that:
  - Calls `extractProblemDetails(error)`.
  - Picks the first available of `detail`, `title`, or the fallback.
  - Maps HTTP 403 to a "You don't have permission to …" message.
  - Maps HTTP 409 to a conflict-specific message.
  - Emits a red Mantine notification.
- Each mutation passes a short, action-specific fallback (e.g. "Failed to
  create stream").

### Out of scope

- Do not change `onSuccess`. Do not refactor success notifications.
- Do not introduce a global mutation-error handler via `QueryClient` defaults
  — per-mutation fallback messages are more user-friendly and the current
  handful of mutations makes a central handler unnecessary.
- Do not change component-level `try/catch` around `mutateAsync` unless it
  actively double-notifies.

## Files expected to change

- `src/NatsManager.Frontend/src/shared/notifications.ts` — new helper.
- Every `features/**/hooks/use*.ts` containing a `useMutation`.
- Associated Vitest test files if they assert on current behaviour.

## Acceptance criteria

- [ ] Every `useMutation` in `features/` has an `onError` handler.
- [ ] Failed mutations produce a red notification whose body text comes from
      the backend `ProblemDetails` when available.
- [ ] 403 responses use a distinct "permission" message (ties in with the
      new `Forbidden` outcome added in Phase 3).
- [ ] `npm test` passes (update tests that asserted "no notification on
      error" — there should be very few, if any).
- [ ] `npm run lint` passes.

## Test plan

1. Add one Vitest test per feature that mocks a failing mutation and
   asserts the notification shim is called with a red notification.
2. Manual smoke via `aspire run`: disable the backend or force a 500, then
   trigger a mutation on each feature and confirm the notification appears.

## Risks / notes

- Some components already call `mutation.mutateAsync` inside a `try/catch`
  that shows its own notification. Audit these — adding `onError` on the
  hook would cause a double-notification. Prefer the hook-level handler and
  remove the duplicate in the component.
- Keep error messages user-facing, not raw stack traces.

## Reference instructions

- `.github/instructions/frontend.instructions.md`
- `.github/instructions/frontend-tests.instructions.md`

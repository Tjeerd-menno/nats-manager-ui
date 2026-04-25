# Deferred Work — Plan Index

These plans were deferred from the three-phase code-review follow-up
(`copilot/code-review-design-patterns-security-dotnet`). Each file is a
self-contained brief that a dedicated agent session (or human contributor)
can pick up without rereading the originating conversation.

Conventions:

- Each plan is scoped so it can ship as a **single small PR**.
- File paths, command names, and acceptance criteria are concrete.
- Plans describe **what** to do, not how to code it. Implementers should
  re-read the relevant `.github/instructions/*.instructions.md` before
  starting.

## Backend

| # | Plan | Summary |
|---|------|---------|
| B1 | [split-jetstream-adapter.md](./split-jetstream-adapter.md) | Split `JetStreamAdapter` (implements both `IJetStreamAdapter` and `IJetStreamWriteAdapter`) into two concrete classes so read and write responsibilities are isolated. |

## Frontend

| # | Plan | Summary |
|---|------|---------|
| F1 | [frontend-query-key-factories.md](./frontend-query-key-factories.md) | Replace the string-literal `queryKey` arrays scattered across feature hooks with per-feature query-key factories. |
| F2 | [frontend-mutation-onerror.md](./frontend-mutation-onerror.md) | Add consistent `onError` notifications to every TanStack Query mutation so failures surface to the user instead of being silently swallowed. |
| F3 | [frontend-axios-timeout.md](./frontend-axios-timeout.md) | Configure a request timeout on the shared `apiClient` so pages can't hang indefinitely on a stalled backend or NATS connection. |
| F4 | [frontend-errorboundary-route-reset.md](./frontend-errorboundary-route-reset.md) | Reset `<ErrorBoundary>` state automatically on route change so navigating away from a crashed page doesn't leave the error visible. |
| F5 | [frontend-role-constants.md](./frontend-role-constants.md) | Replace hard-coded `'Administrator'` / `'Auditor'` strings with a typed constant module shared between `App.tsx`, `AppLayout.tsx`, and `AuthProvider`. |
| F6 | [frontend-inactivity-logout.md](./frontend-inactivity-logout.md) | Add a client-side inactivity-logout hook that pairs with the 30-minute server session idle window introduced in Phase 3. |

## Suggested ordering

1. **F5** (role constants) — tiny, unblocks F6.
2. **F3** (axios timeout) — tiny, self-contained.
3. **F4** (ErrorBoundary reset) — tiny, self-contained.
4. **F1** (query-key factories) — prerequisite for cleaner F2.
5. **F2** (mutation `onError`) — benefits from F1 but not blocked by it.
6. **F6** (inactivity logout) — depends on F5 for role constants only tangentially; otherwise standalone.
7. **B1** (JetStream adapter split) — independent of all frontend work.

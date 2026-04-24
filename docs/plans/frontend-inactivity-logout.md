# F6 — Client-side inactivity-logout hook

## Context

Phase 3 shortened the server session idle window to 30 minutes
(`src/NatsManager.Web/Program.cs` — `AddSession`). The frontend currently
does nothing proactive: when the session expires on the server, the user
only finds out on the next request that gets a 401 (the axios interceptor
in `src/NatsManager.Frontend/src/api/client.ts` then redirects to
`/login`). Until that request happens, a physically unattended browser
keeps displaying sensitive data.

## Goal

A client-side inactivity timer that logs the user out locally (clears
`AuthProvider` state and navigates to `/login`) after a configurable
idle interval, and warns the user shortly before logout with a chance to
stay signed in.

## Scope

### In scope

1. Add `src/NatsManager.Frontend/src/features/auth/useInactivityLogout.ts`
   — a hook that:
   - Accepts `{ idleMs, warnBeforeMs }` (defaults: 30 minutes / 1 minute).
   - Listens on `mousemove`, `keydown`, `click`, and `visibilitychange`
     (but only resets the timer, never fires on them directly). Debounce
     to avoid thrashing.
   - When `idleMs - warnBeforeMs` elapses without activity, surfaces a
     Mantine modal (or notification) "You will be signed out in 1 minute."
     with a "Stay signed in" button that:
       - Resets the timer.
       - Pings `GET /api/auth/me` so the server session slides.
   - When `idleMs` elapses, calls `logout()` from `useAuth` and navigates
     to `/login`.
   - Cleans up all listeners on unmount.
2. Mount the hook inside `AuthProvider` (or a thin child of it) so it's
   active on every authenticated page and skipped on `/login`.
3. Keep the thresholds in a single `const` so they stay in sync with the
   server-side session timeout.
4. Accessibility: the warning modal must be keyboard-dismissible and not
   trap focus permanently.

### Out of scope

- Cross-tab synchronisation (BroadcastChannel) — nice-to-have, defer.
- Server-side push notification of upcoming expiry.

## Files expected to change

- `src/NatsManager.Frontend/src/features/auth/useInactivityLogout.ts` — new.
- `src/NatsManager.Frontend/src/features/auth/AuthProvider.tsx` — mount the
  hook.
- Possibly a small component
  `features/auth/InactivityWarningModal.tsx`.
- Tests covering timer fires, user-activity reset, and "Stay signed in"
  reset.

## Acceptance criteria

- [ ] After `idleMs` of no pointer/keyboard events on an authenticated
      page, the user is redirected to `/login` and `AuthProvider` state is
      cleared.
- [ ] The warning dialog appears at `idleMs - warnBeforeMs` and can be
      dismissed to extend the session by pinging `/auth/me`.
- [ ] The hook is a no-op on `/login` and on non-authenticated state.
- [ ] Vitest uses `vi.useFakeTimers()` to cover all three transitions
      (warn, reset, logout).
- [ ] `npm run lint` and `npm test` pass.

## Test plan

1. Unit test: fake timers, mount the hook inside a test component, assert
   logout fires after `idleMs`.
2. Unit test: after warning fires, dispatching a `mousemove` on `window`
   cancels the pending logout.
3. Manual smoke: set `idleMs = 30 s` locally, leave the page idle, confirm
   warn → logout flow.

## Risks / notes

- Depends on F5 for clean role-string handling but not blocked by it.
- The warning modal should not block the user's ability to navigate — use
  a non-modal Mantine `Notification` if you prefer, but a modal gives a
  clearer "Stay signed in" affordance.
- Make sure the event listeners use `{ passive: true }` to avoid scroll
  jank.

## Reference instructions

- `.github/instructions/frontend.instructions.md`
- `.github/instructions/frontend-tests.instructions.md`

# F4 — Reset `<ErrorBoundary>` on route change

## Context

`src/NatsManager.Frontend/src/shared/ErrorBoundary.tsx` is a standard class
component that stores the caught error in state. It already exposes a "Try
again" button which clears the error, but if the user instead navigates to
a different route, the boundary **keeps** the old error and the destination
page is hidden behind it.

The app uses `react-router` (see `src/NatsManager.Frontend/src/App.tsx`).

## Goal

When the route changes, the boundary's error state is cleared automatically
so the new page renders normally.

## Scope

### In scope

Two acceptable shapes — pick the simpler one:

1. **Wrapper with `useLocation` + `key` prop.** Create a functional wrapper
   that reads `useLocation().pathname` from `react-router` and passes it as
   `key` to the existing class boundary. React unmounts/remounts on `key`
   change, which resets state for free.
2. **`resetOnLocationChange` prop.** Add an effect inside a functional
   sibling that calls a `reset()` method when `pathname` changes. Requires
   exposing `reset` on the boundary.

Use shape 1 unless there's a concrete reason not to — it's one component
and no new public surface.

### Out of scope

- Do not change the boundary's fallback UI.
- Do not introduce `react-error-boundary` as a dependency — the existing
  class is fine.

## Files expected to change

- `src/NatsManager.Frontend/src/shared/ErrorBoundary.tsx` — either export a
  new wrapper or accept a new prop.
- Any call site that should use the new wrapper (check `App.tsx` and
  `AppLayout.tsx`).
- New/updated Vitest test `ErrorBoundary.test.tsx` asserting the state
  resets when the key/location changes.

## Acceptance criteria

- [ ] Navigating from a crashed page to a healthy route shows the healthy
      page (no lingering red alert).
- [ ] Clicking "Try again" still works on the same route.
- [ ] `npm test` passes, including a new test that renders a component
      which throws, advances the router to a new path, and asserts the
      boundary no longer shows the error UI.

## Test plan

1. Vitest: use `MemoryRouter` with a `Routes` tree; mount a component that
   throws on the first path; programmatically navigate; assert the healthy
   component renders.
2. Manual smoke: introduce a throwing component temporarily, navigate
   around to verify.

## Reference instructions

- `.github/instructions/frontend.instructions.md`
- `.github/instructions/frontend-tests.instructions.md`

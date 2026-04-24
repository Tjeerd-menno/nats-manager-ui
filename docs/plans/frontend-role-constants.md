# F5 — Replace hard-coded role strings with typed constants

## Context

Role names are hard-coded across the frontend:

- `src/NatsManager.Frontend/src/App.tsx` — `requiredRoles={['Administrator', 'Auditor']}` and `requiredRoles={['Administrator']}`.
- `src/NatsManager.Frontend/src/shared/AppLayout.tsx` — `hasRole('Administrator')`, `hasRole('Auditor')`.
- Various tests reference the same literals.

The backend's canonical list lives at
`src/NatsManager.Domain/Modules/Auth/Role.cs` under
`Role.PredefinedNames.Administrator` / `.Auditor` / etc. The frontend has
no equivalent — renames would silently drift.

## Goal

One typed module on the frontend defines every role name used in the UI.
Every call site imports from it. A rename is a one-line change plus a
compiler error at every stale site.

## Scope

### In scope

1. Add `src/NatsManager.Frontend/src/features/auth/roles.ts` exporting:
   - A `const` object literal with all role names (`Administrator`,
     `Auditor`, `Operator`, `Viewer` — confirm against
     `Role.PredefinedNames` on the backend).
   - A `Role` union type derived from the values.
2. Update `AuthUser.roles` in
   `src/NatsManager.Frontend/src/features/auth/types.ts` to use
   `Role[]` instead of `string[]` (only if feasible without breaking the
   `/auth/me` response type — otherwise keep as `string[]` and cast at the
   boundary with a narrow helper).
3. Update `hasRole` in
   `src/NatsManager.Frontend/src/features/auth/AuthProvider.tsx` to accept
   `Role` (or a union).
4. Replace every literal `'Administrator'` / `'Auditor'` / `'Operator'` /
   `'Viewer'` under `src/NatsManager.Frontend/src` with a reference to the
   constants, **except** inside test fixtures where the value represents a
   server response (leave those as strings but import the constant so a
   rename still trips the check).

### Out of scope

- No backend changes.
- No UI/permission-logic changes.

## Files expected to change

- `src/NatsManager.Frontend/src/features/auth/roles.ts` — new.
- `src/NatsManager.Frontend/src/features/auth/types.ts`
- `src/NatsManager.Frontend/src/features/auth/AuthProvider.tsx`
- `src/NatsManager.Frontend/src/features/auth/AuthContext.ts` — if
  `hasRole` signature lives there.
- `src/NatsManager.Frontend/src/App.tsx`
- `src/NatsManager.Frontend/src/shared/AppLayout.tsx`
- Tests that pass role strings to mocked `AuthProvider` values.

## Acceptance criteria

- [ ] `rg "'Administrator'|'Auditor'|'Operator'|'Viewer'" src/NatsManager.Frontend/src`
      shows only the new `roles.ts` file (and possibly test fixtures that
      now import the constant).
- [ ] `npm run lint` passes.
- [ ] `npm test` passes — adjust test mocks that built `AuthUser` literals
      so they use the new constants.
- [ ] `npm run build` (or `tsc --noEmit`) passes with no new errors.

## Risks / notes

- Keep the values as strings matching the server exactly (case-sensitive).
- If `hasRole` is widened to `Role`, callers that still pass raw strings
  (tests, dynamic role lists) will fail to type-check — adjust those
  callers rather than weakening the type.

## Reference instructions

- `.github/instructions/frontend.instructions.md`

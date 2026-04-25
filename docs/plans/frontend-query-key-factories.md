# F1 — Centralise TanStack Query keys behind per-feature factories

## Context

Every feature hook currently defines its query keys as inline string-literal
arrays, e.g.:

- `['streams', environmentId, params]`
- `['streams', environmentId, streamName]`
- `['consumers', environmentId, streamName, consumerName]`
- `['kv-buckets', environmentId]`, `['kv-keys', …]`, `['kv-key', …]`
- `['object-buckets', environmentId]`, `['objects', …]`
- `['users']`, `['roles']`, `['user-roles', userId]`
- `['audit-events', params]`
- `['dashboard', environmentId]`
- `['core-nats-status', environmentId]`
- `['services', environmentId]`
- `['search', query, resourceType]`, `['bookmarks']`, `['preferences']`

Invalidations inside mutations duplicate these literals (`useJetStream.ts`,
`useKv.ts`, `useObjectStore.ts`, `useAdmin.ts`, `useEnvironments.ts`,
`useSearch.ts`). Only `useEnvironments.ts` uses a single constant
(`ENVIRONMENTS_KEY`) and even that is just a string, not a full factory.

A typo in a mutation's `invalidateQueries` (e.g. `'objects'` vs `'object'`)
silently leaves the cache stale.

## Goal

Introduce **per-feature** query-key factories so every key and every
invalidation goes through a single typed definition. Factories stay local to
the feature; there is no shared global registry.

## Scope

### In scope

For each feature under
`src/NatsManager.Frontend/src/features/<feature>/hooks/`, add a
`queryKeys.ts` (or similar) that exports a factory object. Typical shape:

- A root list key (e.g. `all`).
- Narrower subkeys parameterised by env id / resource id.

Then replace **every** literal array inside that feature's hooks with calls
to the factory. Apply the same treatment inside mutation
`invalidateQueries` calls. A mutation that currently invalidates multiple
sibling keys should use the broadest factory entry that still covers only
what needs invalidating.

Features to cover (one PR per feature is acceptable, or group them):

- `jetstream` (`useJetStream.ts`).
- `kv` (`useKv.ts`).
- `objectstore` (`useObjectStore.ts`).
- `admin` (`useAdmin.ts`).
- `audit` (`useAudit.ts`).
- `dashboard` (`useDashboard.ts`).
- `corenats` (`useCoreNats.ts`).
- `services` (`useServices.ts`).
- `search` (`useSearch.ts`).
- `environments` (`useEnvironments.ts`) — promote existing constant to full factory.

### Out of scope

- No behavioural changes, no new endpoints, no UI changes.
- No cross-feature shared keys. Keep keys feature-local.

## Files expected to change

- `src/NatsManager.Frontend/src/features/<feature>/hooks/queryKeys.ts` —
  new, per feature.
- `src/NatsManager.Frontend/src/features/<feature>/hooks/use*.ts` — every
  `queryKey:` and `invalidateQueries({ queryKey })` updated.
- Co-located tests under `src/NatsManager.Frontend/src/features/**` — only
  if they assert on literal query keys (`useCoreNats.test.ts` does).

## Acceptance criteria

- [ ] `rg "queryKey:\s*\[" src/NatsManager.Frontend/src/features` returns
      zero hits (all now go through factories).
- [ ] `rg "invalidateQueries\(\{\s*queryKey:\s*\[" src/NatsManager.Frontend/src/features`
      returns zero hits.
- [ ] `npm run lint` and `npm test` both pass inside
      `src/NatsManager.Frontend`.
- [ ] TypeScript strict mode still passes (no `as`-based widening to paper
      over factory shape).
- [ ] Mutations that previously invalidated two sibling keys still
      invalidate both (double-check by reading the diff — this is the main
      regression risk).

## Test plan

1. `cd src/NatsManager.Frontend && npm test` — all Vitest suites pass.
2. `npm run lint` — no new warnings.
3. `aspire run` smoke: create/update/delete a stream, KV entry, and object
   to confirm the UI re-fetches the list after each mutation (visual
   check).

## Risks / notes

- The biggest risk is an invalidation no longer matching a query because the
  factory returned a slightly different array shape. TanStack Query uses
  prefix matching, so make sure the factory's broad key is a prefix of its
  narrower keys.
- Keep factories as `const` objects returning tuples/arrays so TypeScript
  infers literal types.

## Reference instructions

- `.github/instructions/frontend.instructions.md`
- `.github/instructions/frontend-tests.instructions.md`

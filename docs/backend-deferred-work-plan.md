# Backend Deferred Work — Implementation Plan

> Created: 2026-05-30
> Origin: Follow-up to the backend code review (`tjeerd-menno/backend-code-review`).
> Scope: The four review items intentionally **deferred** because they could not be
> completed under the review's hard constraint of *"no functional/behavioral change"*.
> This document is the plan to execute them as deliberate, separately-reviewed changes.

## Why these were deferred

The original review mandate was *"improve maintainability… all without affecting functionality."*
Each item below **necessarily changes observable behavior** (S1/S2) or carries **high regression
risk with no functional benefit** (M1/M7). They were therefore split out so they can be planned,
estimated, and reviewed as intentional behavioral changes rather than smuggled into a
"no-change" refactor.

| ID | Title | Type | Behavioral change? | Risk | Est. |
|----|-------|------|--------------------|------|------|
| S1 | Env-scoped read authz in `MonitoringHub` | Security | **Yes** (some users lose access) | Medium | 1–2 d |
| S2 | Scope CoreNats read endpoints | Security | **Yes** (some users lose access) | Medium | 1–2 d |
| M1 | Standardize cluster query handlers to `IUseCase` | Maintainability | Yes, unless port extended | Medium | 1 d |
| M7 | Split god-classes (`RelationshipProjectionService`, `NatsClusterMonitoringHttpAdapter`) | Maintainability | No (pure refactor) | High | 2–3 d |

> **Recommended order:** decide the authorization model first (S1 + S2 share it), then M1,
> then M7. S1 and S2 should ship together because they must apply a *single consistent*
> read-authorization rule across transports (SignalR + HTTP).

---

## Shared prerequisite — define the read-authorization model (S1 + S2)

Today the codebase has **two** levels of access:

- `[Authorize]` / `.RequireAuthorization()` — any authenticated user.
- Env-scoped *write* protection — `HighImpactActionGuard` (production + `Administrator`
  for that environment) and the `OperatorAccess` policy on `POST /publish`.

There is **no env-scoped _read_ authorization**: any authenticated user can read any
environment's monitoring metrics, cluster topology, core-NATS status, subjects, clients,
and the SSE message stream. S1 and S2 close that gap.

The infrastructure to do this **already exists** and should be reused — do **not** invent a
new mechanism:

- `ScopedRoleClaims.IsInRoleForEnvironment(user, role, environmentId)` — claim-based check.
- `EnvironmentScopedRoleRequirement` + `EnvironmentScopedRoleAuthorizationHandler` — a policy
  handler that already resolves the environment id from route values (`envId` / `id` /
  `environmentId`).

### Decision required (gating question)

Before writing code, confirm the intended rule with the product owner:

1. **What role grants read access to an environment?** Options:
   - Any user who holds **any** scoped role for that environment (ReadOnly/Operator/Administrator/Auditor), or
   - A specific minimum role (e.g. the `ReadOnly` role).
2. **Do global (non-scoped) roles bypass the check?** `IsInRoleForEnvironment` already returns
   `true` for any global role match, so a global `Administrator` keeps full access — confirm
   that's desired.
3. **Migration / blast radius:** enumerate which existing users will lose access and confirm
   their scoped-role claims are provisioned before rollout. This is the main behavioral risk.

Record the answer in this document before implementing, because S1 and S2 must enforce the
**same** rule.

### Suggested policy

Add a single named read policy and reuse it for both HTTP and SignalR:

```csharp
// AuthorizationPolicyNames.cs
public const string EnvironmentReadAccess = "EnvironmentReadAccess";

// DI registration (where policies are configured)
options.AddPolicy(AuthorizationPolicyNames.EnvironmentReadAccess, policy =>
    policy.AddRequirements(new EnvironmentScopedRoleRequirement(
        Role.PredefinedNames.ReadOnly,
        Role.PredefinedNames.Operator,
        Role.PredefinedNames.Administrator,
        Role.PredefinedNames.Auditor)));
```

> The predefined role names are `ReadOnly`, `Operator`, `Administrator`, `Auditor`
> (`NatsManager.Domain.Modules.Auth.Role.PredefinedNames`) — there is **no** `Viewer`.
> Confirm which of these should grant read access before wiring this up.

---

## S2 — Scope CoreNats read endpoints

**File:** `src/NatsManager.Web/Endpoints/CoreNatsEndpoints.cs`

### Current state

```csharp
var group = app.MapGroup("/api/environments/{envId:guid}/core-nats")
    .WithTags("Core NATS")
    .RequireAuthorization();                 // any authenticated user

group.MapGet("/status", GetStatus);          // read — unscoped
group.MapGet("/subjects", GetSubjects);      // read — unscoped
group.MapGet("/clients", GetClients);        // read — unscoped
group.MapPost("/publish", PublishMessage).RequireAuthorization(OperatorAccess); // scoped write
group.MapGet("/stream", StreamMessages);     // read stream — unscoped (only HighImpactActionGuard inside)
```

The three GETs and the SSE `/stream` endpoint do not enforce env-scoped read authorization.
`/stream` runs `HighImpactActionGuard` internally, but that only blocks **production** writes —
it is not a read gate.

### Proposed change

1. Apply the `EnvironmentReadAccess` policy to the **read** endpoints. Because the route
   parameter is `envId`, the existing `EnvironmentScopedRoleAuthorizationHandler` resolves the
   environment automatically.

   Two ways to express it — pick one for consistency:
   - Per-endpoint: `group.MapGet("/status", GetStatus).RequireAuthorization(EnvironmentReadAccess);`
     (and likewise for `/subjects`, `/clients`, `/stream`).
   - Or split into two sub-groups (reads require `EnvironmentReadAccess`, writes keep their
     stricter policies). A sub-group keeps the mapping table readable.

2. Leave `/publish` as-is (it already has `OperatorAccess` + `HighImpactActionGuard`). Confirm
   that operators/admins also implicitly satisfy read access (they will, if those roles are in
   the `EnvironmentReadAccess` requirement).

3. Re-evaluate whether `/stream`'s internal `HighImpactActionGuard` is still wanted. Reading an
   SSE message stream is arguably a *read*, not a high-impact action. Decide explicitly:
   keep it (defensive, unchanged behavior for production) or drop it once read-scoping lands.
   Document the decision.

### Tests (`tests/NatsManager.Web.Tests`)

- A user **with** a scoped read role for the env → `200` on status/subjects/clients, stream opens.
- A user **without** any role for the env → `403` on all four read endpoints.
- A global admin → still `200` (regression guard for the bypass rule).
- Existing publish tests must remain green.

### Acceptance criteria

- All CoreNats reads require env-scoped read access via the shared policy.
- No change to response *shape* — only the set of callers who receive `403` changes.
- OpenAPI/Swagger still advertises the endpoints (auth is enforced at runtime, not by removal).

---

## S1 — Env-scoped read authorization in `MonitoringHub`

**File:** `src/NatsManager.Web/Hubs/MonitoringHub.cs`

### Current state

```csharp
public async Task SubscribeToEnvironment(string environmentId)
{
    if (!Guid.TryParse(environmentId, out var id))
        throw new HubException("Invalid environment id.");

    _ = await environmentRepository.GetByIdAsync(id, Context.ConnectionAborted)
        ?? throw new HubException("Environment not found.");   // existence check only

    await Groups.AddToGroupAsync(Context.ConnectionId, $"env-{id}", Context.ConnectionAborted);
    // ... sends latest snapshot
}
```

The hub has `[Authorize]` (authenticated) but **any** authenticated user can join **any**
environment group and receive its live metrics. There is no per-environment check before
`AddToGroupAsync`.

### Why a policy attribute is not enough

`[Authorize(Policy = ...)]` on a hub **method** runs the policy handler, but the
`EnvironmentScopedRoleAuthorizationHandler` resolves the environment from **HTTP route values**,
which are not populated for a SignalR hub-method invocation (the env id arrives as a **method
argument**). So the existing handler cannot see the environment id here. Two options:

- **Option A (recommended): explicit in-method check.** Reuse `ScopedRoleClaims` directly inside
  `SubscribeToEnvironment` — it's the lowest-risk, most readable approach for a hub.

  ```csharp
  if (!Context.User!.IsInRoleForEnvironment(Role.PredefinedNames.ReadOnly, id)
      && !Context.User.IsInRoleForEnvironment(Role.PredefinedNames.Operator, id)
      && !Context.User.IsInRoleForEnvironment(Role.PredefinedNames.Administrator, id)
      && !Context.User.IsInRoleForEnvironment(Role.PredefinedNames.Auditor, id))
  {
      throw new HubException("You do not have access to this environment.");
  }
  ```

  Extract the role set into a shared helper so S1 and S2 use the **same** list (e.g. a
  `EnvironmentAccess.CanRead(ClaimsPrincipal, Guid)` extension in `Web/Security`), keeping the
  rule in one place.

- **Option B: a resource-based authorization service.** Inject `IAuthorizationService` and call
  `AuthorizeAsync(Context.User, id, EnvironmentReadAccess)` with a requirement/handler that reads
  the **resource** argument instead of route values. More plumbing; only worth it if you want the
  policy name to be the single source of truth across HTTP + SignalR.

Recommendation: **Option A** with a shared `CanRead` helper. It keeps the hub simple and still
centralizes the rule.

### Additional hardening (optional, same change-set)

- `UnsubscribeFromEnvironment` needs no authz (removing yourself from a group is harmless), but
  keep it consistent if reviewers prefer.
- Consider validating the env **exists** *after* the authz check to avoid leaking existence to
  unauthorized callers (authz-before-existence ordering).

### Tests (`tests/NatsManager.Web.Tests`)

- Hub test: authorized user (scoped role) → joins group, receives snapshot.
- Hub test: unauthorized user → `HubException`, **not** added to the group, no snapshot sent.
- Global admin → still allowed.
- If the Web tests don't already host SignalR, add a `HubConnection` test against the
  `NatsManagerWebAppFactory`, or unit-test the extracted `CanRead` helper plus a thin hub test
  with a faked `HubCallerContext`.

### Acceptance criteria

- `SubscribeToEnvironment` denies callers lacking read access **before** `AddToGroupAsync`.
- S1 and S2 enforce the *same* role set via the shared helper/policy.

---

## M1 — Standardize cluster query handlers to `IUseCase`

**Files:**
- `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/GetClusterOverviewQuery.cs`
- `src/NatsManager.Application/Modules/Monitoring/Queries/ClusterObservability/GetClusterTopologyQuery.cs`
- `src/NatsManager.Web/Endpoints/MonitoringEndpoints.cs`
- `src/NatsManager.Application/Common/IOutputPort.cs` (only if extending the port)

### Current state & the blocker

These two handlers do **not** follow the project's `IUseCase<TQuery, TResult>` + `IOutputPort`
convention. They expose a synchronous `Handle(query)` returning a **nullable** result, are
`new`-ed directly in the endpoint, and the endpoint maps `null` → **HTTP 503** ("Cluster
Monitoring/Topology Unavailable"):

```csharp
var handler = new GetClusterOverviewQueryHandler(observationStore);
var observation = handler.Handle(new GetClusterOverviewQuery(environmentId));
if (observation is null)
    return Results.Problem(statusCode: 503, title: "Cluster Monitoring Unavailable", ...);
```

`IOutputPort<T>` only models `Success` / `NotFound` (404) / `Conflict` (409) /
`Unauthorized` (401) / `Forbidden` (403). **There is no "service unavailable / 503" outcome.**
So a naïve conversion would map the null case to `NotFound` → **404**, changing the contract
from 503 to 404. That's why this was deferred under the no-behavior-change rule.

### Two viable approaches (pick one)

**Approach 1 — Extend the port with a 503 outcome (preserves the 503 contract).**

1. Add `void Unavailable(string message)` to `IOutputPort<T>`.
2. Implement it in **every** presenter:
   - `Presenter<T>` (`src/NatsManager.Web/Presenters/`) → maps to `Results.Problem(503, ...)`.
   - `TestOutputPort<T>` in the test projects → record the outcome.
   - Any other `IOutputPort` implementers (grep for `: IOutputPort`).
3. Convert both handlers to `IUseCase<TQuery, TResult>`:
   - `ExecuteAsync` calls `outputPort.Success(result)` when data exists, otherwise
     `outputPort.Unavailable("No cluster observation data is available …")`.
4. Register them via DI (they'll be picked up by `AddUseCases`) and inject
   `IUseCase<GetClusterOverviewQuery, ClusterObservationResult>` etc. into the endpoint instead
   of `new`-ing them.
5. Endpoint uses the standard `presenter.ToResult()` / `ExecuteToResultAsync` flow.

- **Pros:** fully consistent with every other handler; removes manual instantiation; testable
  via the standard `TestOutputPort`.
- **Cons:** touches the shared `IOutputPort` contract and all presenters; the 503 semantic is
  unusual enough that reviewers must agree it belongs in the shared port.

**Approach 2 — Keep the 503 in the endpoint, standardize only the shape.**

- Convert handlers to `IUseCase` but have them return a result type that still expresses
  availability (e.g. `Success` with a result whose `Observation` may be null), and keep the
  null→503 decision in the endpoint. This avoids changing `IOutputPort` but leaves a small
  endpoint-side special case.

Recommendation: **Approach 1** if the team accepts a first-class `Unavailable`/503 outcome
(it is genuinely useful elsewhere — any "upstream dependency down" read); otherwise Approach 2.

### Tests

- `tests/NatsManager.Application.Tests` — handler returns `Success` when the store has data;
  emits `Unavailable` (Approach 1) when empty.
- `tests/NatsManager.Web.Tests` — endpoint still returns **503** when no observation exists, and
  **200** + correct body when it does. These existing-contract tests are the regression guard.

### Acceptance criteria

- Both handlers implement `IUseCase` and are DI-injected (no `new` in the endpoint).
- The HTTP contract is unchanged: 200 with body, or 503 when unavailable.
- `AddUseCases` registration scan still excludes generic/decorator types (already handled in L9).

---

## M7 — Split the two god-classes

> ⚠️ **Pure refactor — must not change behavior.** Land behind the full existing test suite and
> add characterization tests first where coverage is thin. This is the highest-risk item by line
> count, lowest by functional impact.

### M7a — `RelationshipProjectionService` (497 lines)

**File:** `src/NatsManager.Infrastructure/Relationships/RelationshipProjectionService.cs`

The class is already decomposed into cohesive private methods implementing a single bounded
BFS-projection algorithm (`TraverseEdgesBfsAsync`, `BuildNodeIds`, `SelectIncludedNodeIds`,
`FilterEdgesByIncludedNodes`, `ResolveNodesAsync`, `RemoveDanglingEdges`,
`PropagateWarningStates`, `ApplyHealthStateFilterAfterPropagation`, …). The "god class" smell is
**length**, not tangled responsibilities — so the win is modest and the risk of breaking the
ordering-sensitive pipeline is real.

**Recommended approach — extract cohesive collaborators, not a big-bang rewrite:**

1. **`EdgeTraversal`** — owns `TraverseEdgesBfsAsync` + edge dedup. Input: focal + filters +
   sources; output: raw edge expansion.
2. **`GraphBounding`** — pure functions for `BuildNodeIds`, `SelectIncludedNodeIds`,
   `FilterEdgesByIncludedNodes`, MaxNodes/MaxEdges truncation, `RemoveDanglingEdges`. These are
   stateless and the easiest, safest extraction (ideal first step).
3. **`WarningPropagation`** — `PropagateWarningStates` +
   `ApplyHealthStateFilterAfterPropagation`.
4. `RelationshipProjectionService` becomes the thin orchestrator (`ProjectAsync`) that wires the
   three collaborators together — the current top-level method already reads as that script.

Keep collaborators `internal` (and `internal sealed`); the public port
(`IRelationshipProjectionService` / `ProjectAsync`) is unchanged. Extract **one** collaborator
per commit, running the full suite between each, so any regression is bisectable.

### M7b — `NatsClusterMonitoringHttpAdapter` (474 lines)

**File:** `src/NatsManager.Infrastructure/Nats/ClusterObservability/NatsClusterMonitoringHttpAdapter.cs`

Read the file first (not yet inspected in detail) and group by responsibility. Typical seams for
a monitoring HTTP adapter:

1. **HTTP transport** — building requests, sending, handling timeouts/cancellation, status-code
   handling. Candidate: `ClusterMonitoringHttpClient`.
2. **Response parsing/mapping** — deserializing `varz`/`connz`/`routez`/`gatewayz`/`leafz`
   (or equivalent) JSON into domain/application models. Candidate: one or more `*Mapper`
   classes (pure, trivially unit-testable).
3. **Aggregation/derivation** — combining endpoint responses into a `ClusterObservation`.

Extract the **mappers first** (pure, no I/O → safest), then the transport, leaving the adapter
as a thin coordinator implementing the port.

### Tests

- Run the **entire** suite between every extraction (Domain 98 / Application 223 /
  Infrastructure 65 / Web 104 baseline — keep all green).
- The Infrastructure integration tests for cluster monitoring require a live NATS server — they
  are **build-verified** in CI-less environments; run them where a broker is available before
  merging M7b.
- Add focused unit tests for the newly-extracted **pure** mappers/bounding functions — this is a
  coverage win the split unlocks.

### Acceptance criteria

- No public API / port signature changes; DI registrations updated only if new types need
  registering (prefer constructing collaborators inside the owning class to avoid DI churn).
- Full test suite green; new unit tests cover extracted pure logic.
- Each class has a single clear responsibility and a meaningfully smaller line count.

---

## Cross-cutting notes

- **Sequencing:** S1+S2 together (shared auth rule) → M1 → M7a → M7b. Independent enough to be
  separate PRs; S1/S2 should be one PR.
- **Behavioral-change items (S1/S2)** need product sign-off and a user-access migration check
  before rollout; ship behind the existing test suite plus the new authz tests.
- **Pure-refactor item (M7)** must be reviewed strictly for "no behavior change" — prefer many
  small commits over one large diff.
- Update `docs/` and any architecture notes if the `IOutputPort` contract gains an `Unavailable`
  outcome (M1, Approach 1).

## Status checklist

- [x] Read-authorization model decided & documented (gating S1/S2)
- [x] S2 — CoreNats read endpoints scoped + tests
- [x] S1 — MonitoringHub read authz + tests
- [x] M1 — cluster handlers → `IUseCase` (+ port decision) + tests
- [x] M7a — `RelationshipProjectionService` split + tests
- [x] M7b — `NatsClusterMonitoringHttpAdapter` split + tests

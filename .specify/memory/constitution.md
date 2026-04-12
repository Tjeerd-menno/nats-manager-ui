<!--
  Sync Impact Report
  ==================================================
  Version change: N/A → 1.0.0 (initial ratification)
  Modified principles: N/A (initial version)
  Added sections:
    - Core Principles (4 principles)
    - Performance Standards
    - Development Workflow & Quality Gates
    - Governance
  Removed sections: N/A
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ compatible (Constitution Check section exists)
    - .specify/templates/spec-template.md ✅ compatible (user stories + acceptance scenarios align)
    - .specify/templates/tasks-template.md ✅ compatible (phased task structure supports all principles)
  Follow-up TODOs: None
  ==================================================
-->

# NATS Management Application Constitution

## Core Principles

### I. Code Quality (NON-NEGOTIABLE)

All code MUST be clean, maintainable, and consistently structured.

- Every module MUST have a single, well-defined responsibility.
- Code MUST follow established project linting and formatting rules;
  no unformatted code may be merged.
- Functions and components MUST be small enough to understand in
  isolation. If a function exceeds ~40 lines, it MUST be decomposed.
- Naming MUST be descriptive and use NATS domain terminology
  accurately (streams, consumers, buckets, subjects — never
  invented synonyms).
- Dead code, unreachable branches, and commented-out blocks MUST
  be removed before merge.
- All public interfaces MUST have explicit type definitions; `any`
  types are forbidden except at verified system boundaries.
- Dependencies MUST be explicitly declared and version-pinned.
  New dependencies require justification in the PR description.
- Code duplication across modules MUST be extracted when the same
  logic appears three or more times.

**Rationale**: The application manages production NATS infrastructure
where bugs have operational consequences. Code quality is the first
line of defense against incidents.

### II. Testing Standards (NON-NEGOTIABLE)

Every feature MUST be verified by automated tests before merge.

- Unit tests MUST cover all business logic, data transformations,
  and validation rules. Minimum coverage target: 80% of new code.
- Integration tests MUST verify interactions with NATS (connection
  management, stream/consumer CRUD, KV/Object Store operations,
  service discovery).
- Contract tests MUST validate API boundaries between frontend
  and backend layers.
- Tests MUST be deterministic — no flaky tests allowed in the main
  branch. A test that fails intermittently MUST be fixed or removed
  within one sprint.
- Destructive operation safeguards (confirmation dialogs, permission
  checks) MUST have dedicated test coverage.
- Test names MUST describe the scenario and expected outcome, not
  the implementation (e.g., "creates stream with retention policy"
  not "test_create_stream_1").
- Mocks MUST be used only at system boundaries (NATS connection,
  HTTP, filesystem). Internal module interactions MUST be tested
  with real implementations.
- All tests MUST pass in CI before a PR can be merged.

**Rationale**: The application performs state-changing operations on
messaging infrastructure. Untested code paths risk data loss,
misconfigured streams, or unintended destructive actions.

### III. User Experience Consistency

The application MUST present a unified, predictable experience
across all NATS capability areas.

- Navigation patterns MUST be consistent across Core NATS,
  JetStream, KV Store, Object Store, and Services views.
- Resource list views MUST use the same layout, sorting, filtering,
  and pagination patterns regardless of resource type.
- Detail views MUST follow a consistent structure: identity →
  status → configuration → relationships → actions.
- State-changing actions MUST be visually distinct from read-only
  operations across all views.
- Destructive operations MUST always require explicit confirmation
  with resource name and environment context displayed.
- Error messages MUST be actionable and consistent in tone and
  format. Every error MUST tell the user what happened and what
  they can do next.
- Loading, empty, error, and stale-data states MUST be handled
  explicitly in every view — no blank screens or silent failures.
- Environment context (which cluster, which account) MUST be
  visible at all times to prevent cross-environment mistakes.
- Terminology MUST match NATS documentation exactly. UI labels
  MUST NOT invent alternative names for NATS concepts.
- Accessibility: interactive elements MUST be keyboard-navigable
  and MUST have appropriate ARIA labels.

**Rationale**: Operators managing production NATS environments
under pressure MUST NOT waste cognitive effort learning different
interaction patterns for different resource types. Consistency
reduces errors and builds trust.

### IV. Performance Requirements

The application MUST remain responsive under realistic operational
load.

- Initial page load MUST complete within 2 seconds on a standard
  connection.
- Navigation between views MUST complete within 500ms (excluding
  network latency to NATS).
- Resource list views MUST handle 1,000+ items without degraded
  scroll or filter performance.
- Real-time data updates (connection status, consumer lag, message
  counts) MUST refresh without full page reloads.
- Search and filter operations MUST return results within 300ms
  for local data and within 1 second for server-side queries.
- The application MUST NOT block the UI thread during API calls;
  all network operations MUST be asynchronous with appropriate
  loading indicators.
- Memory usage MUST remain stable during extended sessions (no
  memory leaks from subscriptions, polling, or component
  lifecycles).
- The application MUST degrade gracefully when a NATS environment
  is slow or unreachable — timeouts MUST be bounded (max 10s) and
  the UI MUST remain interactive.

**Rationale**: Operators use this tool during incidents when speed
matters most. A slow management UI compounds the stress of an
already degraded environment.

## Performance Standards

Quantitative benchmarks for CI and acceptance testing:

| Metric | Target | Measurement |
| --- | --- | --- |
| First Contentful Paint | ≤ 1.5s | Lighthouse CI |
| Time to Interactive | ≤ 2.0s | Lighthouse CI |
| View navigation (client) | ≤ 500ms | Performance marks |
| API response (p95) | ≤ 1s | Server metrics |
| Resource list render (1k items) | ≤ 200ms | Component benchmark |
| JS bundle size (initial) | ≤ 300KB gzipped | Build output |
| Memory growth per hour | ≤ 5MB | Long-session profiling |

Performance regressions that exceed these thresholds MUST block
the PR until resolved or an exception is documented and approved.

## Development Workflow & Quality Gates

All changes MUST pass through the following gates before merge:

1. **Lint & Format**: Automated checks MUST pass with zero
   warnings. No lint-disable comments without justification.
2. **Type Check**: Full type check MUST pass with strict mode
   enabled.
3. **Unit Tests**: All unit tests MUST pass. Coverage MUST NOT
   decrease for modified files.
4. **Integration Tests**: All integration tests MUST pass against
   a running NATS test instance.
5. **Performance Check**: Lighthouse CI scores MUST meet the
   thresholds defined in Performance Standards.
6. **Code Review**: At least one approval required. Reviewer MUST
   verify UX consistency with existing patterns before approving
   UI changes.
7. **Constitution Compliance**: Reviewer MUST confirm changes do
   not violate any principle in this constitution.

Hotfixes for production incidents may bypass gate 5 (Performance
Check) but MUST file a follow-up ticket to verify performance
within 48 hours.

## Governance

This constitution is the authoritative source of project standards.
It supersedes informal agreements, ad-hoc decisions, and individual
preferences.

- **Amendments** require a PR with rationale, impact analysis, and
  at least two approvals from core contributors.
- **Versioning** follows semantic versioning:
  - MAJOR: principle removal or redefinition
  - MINOR: new principle or materially expanded guidance
  - PATCH: clarifications, wording, typos
- **Compliance review**: every sprint retrospective MUST include a
  brief constitution compliance check.
- **Exceptions**: any deviation from this constitution MUST be
  documented in the PR description with explicit justification and
  a remediation plan.

**Version**: 1.0.0 | **Ratified**: 2026-04-05 | **Last Amended**: 2026-04-05

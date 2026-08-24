# Workflow Security Baseline

Last updated: 2026-08-24
Owner: Repository maintainers (`@Tjeerd-menno`)

## 1) Scope and target workflow set

Target set is all executable workflow files in:

- `.github/workflows/*.yml`

Current target files:

- `.github/workflows/pr-actions.yml`
- `.github/workflows/e2e-nightly.yml`
- `.github/workflows/release.yml`
- `.github/workflows/dependabot-auto-merge.yml`
- `.github/workflows/workflow-security-policy.yml`

## 2) Generated lock workflow handling

No generated lock workflows remain in `.github/workflows/`. The agentic workflows that produced them
(the gh-aw `daily-repo-status` pair and the squad orchestration workflows) were removed on 2026-08-24,
along with the squad tooling under `.squad/` and `.copilot/` that supplied their template mirrors.
The policy gate still enforces the lock/source drift rule, so any future `*.lock.yml` must be changed
only through its source/compile flow.

## 3) Baseline findings (initial posture)

| ID | Finding | Severity | Owner | Baseline status | Current status |
|---|---|---|---|---|---|
| WF-001 | Floating action tags in non-generated workflows | High | Repo maintainers | Open | Closed |
| WF-002 | Checkout steps without explicit `persist-credentials: false` in non-push jobs | Medium | Repo maintainers | Open | Closed |
| WF-003 | PAT fallback to `GITHUB_TOKEN` in privileged assignment path | High | Repo maintainers | Open | Closed (workflow removed) |
| WF-004 | Dependabot auto-merge lacked explicit policy labels and required-check gating | High | Repo maintainers | Open | Closed |
| WF-005 | No CI policy gate for workflow pinning/credential persistence/forbidden patterns | High | Repo maintainers | Open | Closed |
| WF-006 | Manual drift risk for generated lock workflow artifacts | Medium | Repo maintainers | Open | Closed |

## 4) Current controls

- Non-generated workflows use SHA-pinned actions.
- Checkout steps in non-push workflows explicitly set `persist-credentials: false`.
- Dependabot auto-merge is constrained to approved ecosystems (`github-actions`, `nuget`, `npm`), explicit label, branch protection, and successful required checks.
- `workflow-security-policy.yml` enforces:
  - SHA pinning for actions in non-generated workflows
  - `persist-credentials: false` for checkout usage
  - no token fallback pattern (`secretA || secrets.GITHUB_TOKEN`)
  - no `pull_request_target`
  - no mutable image tags (`image: <name>:<tag>`)
  - generated lock/source drift guard

## 5) Verification baseline

- Frontend lint/tests: pass after dependency install (`npm ci --ignore-scripts`, `npm run lint`, `npm test`)
- `dotnet test`: has pre-existing integration fixture timeout failures unrelated to workflow file changes

## 6) Review cadence and governance

- Workflow security owner: Repository maintainers (`@Tjeerd-menno`)
- Review cadence: bi-weekly
- Pin refresh cadence: monthly (or faster for critical advisories), via normal PR review
- Exception handling: document accepted risk in PR description and link to tracking issue
- Next review date: 2026-06-12

# Workflow Security Baseline

Last updated: 2026-05-12
Owner: Repository maintainers (`@Tjeerd-menno`)

## 1) Scope and target workflow set

Target set is all executable workflow files in:

- `.github/workflows/*.yml`

Current target files:

- `.github/workflows/pr-actions.yml`
- `.github/workflows/release.yml`
- `.github/workflows/dependabot-auto-merge.yml`
- `.github/workflows/squad-heartbeat.yml`
- `.github/workflows/sync-squad-labels.yml`
- `.github/workflows/squad-issue-assign.yml`
- `.github/workflows/squad-triage.yml`
- `.github/workflows/workflow-security-policy.yml`
- `.github/workflows/daily-repo-status.lock.yml` (generated)

Generated lock artifacts are excluded from manual edits and must be changed through their source/compile flow.
Non-executable workflow template mirrors under `.squad/templates/workflows/` are kept in sync when related workflows change, but they are not part of the required policy-enforcement scope because the remediation target is executable `.github/workflows/*.yml` files.

## 2) Generated lock workflow handling

- Generated lock workflow: `.github/workflows/daily-repo-status.lock.yml`
- Source: `.github/workflows/daily-repo-status.md`
- Regeneration command: `gh aw compile`
- Dependabot is configured to ignore direct updates to `github/gh-aw-actions` for the lock artifact.
- CI policy enforces that lock changes require corresponding source changes.

## 3) Baseline findings (initial posture)

| ID | Finding | Severity | Owner | Baseline status | Current status |
|---|---|---|---|---|---|
| WF-001 | Floating action tags in non-generated workflows | High | Repo maintainers | Open | Closed |
| WF-002 | Checkout steps without explicit `persist-credentials: false` in non-push jobs | Medium | Repo maintainers | Open | Closed |
| WF-003 | PAT fallback to `GITHUB_TOKEN` in privileged assignment path | High | Repo maintainers | Open | Closed |
| WF-004 | Dependabot auto-merge lacked explicit policy labels and required-check gating | High | Repo maintainers | Open | Closed |
| WF-005 | No CI policy gate for workflow pinning/credential persistence/forbidden patterns | High | Repo maintainers | Open | Closed |
| WF-006 | Manual drift risk for generated lock workflow artifacts | Medium | Repo maintainers | Open | Closed |

## 4) Current controls

- Non-generated workflows use SHA-pinned actions.
- Checkout steps in non-push workflows explicitly set `persist-credentials: false`.
- Privileged Copilot assignment requires `COPILOT_ASSIGN_TOKEN` and no fallback.
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

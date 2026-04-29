---
description: |
  This workflow creates daily repo status reports. It gathers recent repository
  activity (issues, PRs, discussions, releases, code changes) and generates
  engaging GitHub issues with productivity insights, community highlights,
  and project recommendations.

on:
  schedule: daily
  workflow_dispatch:

permissions:
  contents: read
  issues: read
  pull-requests: read

network: defaults

tools:
  github:
    # If in a public repo, setting `lockdown: false` allows
    # reading issues, pull requests and comments from 3rd-parties
    # If in a private repo this has no particular effect.
    lockdown: false
    min-integrity: none # This workflow is allowed to examine and comment on any issues

safe-outputs:
  mentions: false
  allowed-github-references: []
  create-issue:
    title-prefix: "[repo-status] "
    labels: [report, daily-status]
    close-older-issues: true
source: githubnext/agentics/workflows/daily-repo-status.md@aae6af93b4035df74630817afa13c528d6b74fe6
---

# Daily Repo Status

Create an upbeat daily status report for the repo as a GitHub issue.

## What to include

- Recent repository activity (issues, PRs, discussions, releases, code changes)
- Progress tracking, goal reminders and highlights
- Project status and recommendations
- Actionable next steps for maintainers

## Roadmap accuracy requirements

- Treat future-looking sections such as "What's on the Horizon" and "Recommended Next Steps" as a verified gap analysis, not a generic wishlist.
- Inspect the current repository state before recommending work, including relevant code under `src/`, plans/specs under `specs/`, and recently merged PRs.
- Do not suggest an area as upcoming work if the repository already contains shipped routes, pages, endpoints, or adapters for it. Mention that work under shipped progress instead.
- Prefer recommendations that are clearly still incomplete, partially implemented, spec-only, or missing entirely.
- When helpful, briefly explain why a recommendation is still outstanding (for example: spec exists but implementation is missing, backend exists without UI, or the feature is only partially wired).

## Style

- Be positive, encouraging, and helpful 🌟
- Use emojis moderately for engagement
- Keep it concise - adjust length based on actual activity

## Process

1. Gather recent activity from the repository
2. Study the repository, its issues, pull requests, and current implementation status
3. Verify every future-looking recommendation against the actual codebase so you only propose work that still appears to be unimplemented or incomplete
4. Create a new GitHub issue with your findings and insights

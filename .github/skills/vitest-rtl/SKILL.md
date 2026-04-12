---
name: vitest-rtl
description: Generate, review, and fix unit tests in Smart Lab UI using Vitest and React Testing Library.
---

# Vitest + React Testing Library Skill for Smart Lab UI

## Purpose

Generate or update production-quality unit and component tests for Smart Lab UI using Vitest and React Testing Library.

## Scope

- Work only in `smart-lab-ui`
- Use this skill for `.test.ts`, `.test.tsx`, `.spec.ts`, and `.spec.tsx` files
- Do not introduce Jest-specific APIs or patterns
- Do not modify backend code for UI-only test tasks

## Required workflow

Before generating or changing tests:

1. Inspect the nearest existing tests for the same app, library, or feature area
2. Inspect the component, hook, utility, or module under test
3. Reuse existing test utilities, custom render helpers, providers, and mocks
4. Summarize the existing patterns you will follow
5. Only then generate or update the test

## Non-negotiable rules

- Never invent helpers, providers, wrapper utilities, or mock modules that do not exist in the repo
- Prefer user-facing assertions over implementation-detail assertions
- Prefer React Testing Library queries in this order:
  1. `getByRole`
  2. `getByLabelText`
  3. `getByPlaceholderText`
  4. `getByText`
  5. existing repo-approved helpers
- Avoid querying by class name, DOM structure, or implementation-only attributes
- Do not test internal state or private implementation details
- Do not over-mock React behavior
- Mock only external boundaries such as API calls, router integrations, or global browser APIs when needed
- Keep comments to a minimum
- Generated tests must read like production code written by a senior engineer: minimal, clear, and free of unnecessary comments

## Vitest rules

- Use Vitest APIs already used in the repo
- Prefer `vi.mock`, `vi.fn`, and existing repo mock patterns
- Reuse existing setup files and test configuration
- Do not introduce Jest globals or Jest-specific matchers unless the repo already supports them through compatibility layers

## React Testing Library rules

- Test behavior as a user experiences it
- Prefer assertions from visible output, accessibility roles, and interaction results
- Use `userEvent` or the repo’s existing interaction helper pattern if already present
- Avoid asserting on component internals, hook internals, or exact DOM shape unless that is the established pattern in the repo

## Output style

- Generate production-ready test code, not tutorial-style code
- Keep comments to a minimum
- Do not narrate the test flow
- Prefer expressive test names over inline comments

## Checklist

Use `checklist.md` as a required quality gate before finalizing any generated test.

## Validation

When validating generated unit tests:

1. Determine the Nx project name from the file path:
   - `smart-lab-ui/apps/speedyglove/...` → `speedyglove`
   - `smart-lab-ui/apps/dose-app/...` → `dose-app`
   - `smart-lab-ui/libs/...` → find the corresponding project name from existing tests or project.json

2. Run:

`pnpm exec nx run <project-name>:test`

3. Always execute from the `smart-lab-ui` root

If execution is not possible:
- do not pretend tests were run
- provide the exact command instead
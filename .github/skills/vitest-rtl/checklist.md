# Vitest + React Testing Library Checklist

## Before writing the test

- [ ] I reviewed the nearest existing tests for this feature/app/lib
- [ ] I reused existing test helpers, render utilities, and provider wrappers where available
- [ ] I matched the repo’s existing Vitest mocking style

## Query and assertion quality

- [ ] I used user-facing queries first: role, label, placeholder, text
- [ ] I did not rely on class names, DOM shape, or implementation-only selectors
- [ ] I asserted behavior that matters to users
- [ ] I did not assert internal component state or implementation details

## Mocking

- [ ] I mocked only external boundaries where necessary
- [ ] I did not invent mock helpers or providers
- [ ] I reused existing repo patterns for `vi.mock` and setup

## Code quality

- [ ] The test is readable without heavy comments
- [ ] Comments are only used for non-obvious workarounds
- [ ] The test reads like production code written by a senior engineer
- [ ] The test matches the existing style of neighboring tests

## Honesty

- [ ] I did not guess missing providers, helpers, or context setup
- [ ] If the component cannot be tested reliably with the current setup, I called out the gap explicitly
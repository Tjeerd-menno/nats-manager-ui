---
description: "Use when working with SpecKit artifacts, feature specifications, implementation plans, or task files. Covers the spec-to-implementation workflow and artifact locations."
applyTo: "specs/**"
---
# Specification & Planning Instructions

## SpecKit Workflow

Features follow the SpecKit pipeline: **specify → plan → tasks → implement**.

Use the corresponding agents:
- `speckit.specify` — create/update feature spec from description
- `speckit.plan` — generate implementation plan from spec
- `speckit.tasks` — generate dependency-ordered task list
- `speckit.implement` — execute tasks
- `speckit.clarify` — ask clarification questions about underspecified areas
- `speckit.analyze` — cross-artifact consistency check

## Artifact Location

All spec artifacts live under `specs/{feature-id}/`:

```
specs/001-nats-admin-app/
├── spec.md              # Feature specification with user stories
├── plan.md              # Implementation plan
├── data-model.md        # Entity descriptions and invariants
├── research.md          # Background research
├── quickstart.md        # Getting started guide
├── checklists/          # Requirement checklists
└── contracts/           # API contracts
```

## Root Spec Files

- `nats-management-functional-spec.md` — Functional requirements
- `nats-management-technical-architecture.md` — Architecture document (DDD, Clean Architecture, CQRS)

## Conventions

- Feature IDs: zero-padded numeric prefix (`001-`, `002-`, etc.)
- Specs reference user stories as P1–P8 priorities
- Data models define aggregate invariants explicitly

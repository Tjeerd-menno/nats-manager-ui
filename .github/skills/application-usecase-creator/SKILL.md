---
name: application-usecase-creator
description: Create or update SmartLab Application layer use case implementations using existing service patterns (commands/queries, output ports, repositories, event store usage, DI registration, and tests). Use when implementing a new UseCase in `SmartLab.{Service}.Application` or refactoring an existing one. Always include audit trail publication for actions that change configuration state and for use cases that access sensitive or PII data.
---

# Application UseCase Creator

Implement use cases by matching the target service's existing style and boundaries.

## Workflow

1. Inspect neighboring use cases in the same service and feature folder.
2. Match local structure (`Commands/` vs `Queries/`, folder depth, and file naming).
3. Define or update the use case interface and input/output contracts.
4. Implement orchestration logic in the use case class.
5. Publish audit trail entries where required.
6. Update DI registration in the Application layer extension.
7. Add or update tests in the service test project.

## Match existing service style first

- Reuse existing constructor style in that service (`class X(...)` primary constructor or classic constructor).
- Reuse the local dependency style:
	- Repository-based services (`I{Entity}Repository`).
	- Event-sourced services (`IEventStoreRepository<TAggregate, Guid>` plus projection/query delegates).
- Reuse local output style:
	- Output port pattern for business outcomes (`I{UseCase}OutputPort`).
	- Direct DTO return only if that service already uses it.
- Keep logging and message wording aligned with nearby use cases.

## Implement use case contracts

- Keep interface and implementation signatures aligned with local conventions.
- Include `CancellationToken` when the local feature already uses async cancellation.
- Keep input/output DTO naming consistent (`{UseCase}Input`, `{Thing}IdDto`, etc.).
- Prefer explicit business outcomes through output ports rather than exception-driven control flow.

## Implement use case logic

- Orchestrate domain/repository calls; do not move infrastructure concerns into Application.
- Validate business preconditions and return early through output ports on failures.
- Persist state changes before publishing follow-up messages/events when local code does so.
- Publish downstream projection/message updates if the feature pattern requires it.

Detailed patterns and examples: [references/usecase-patterns.md](references/usecase-patterns.md).

## Apply required audit trail rules

Publish an audit trail entry in all of these cases:

- Any action that changes configuration state (add/update/delete/enable/disable/toggle).
- Any use case that accesses sensitive or PII data, including read access.

Implementation guidance:

- Use the service-local audit abstraction (commonly `IAuditTrailOutputPort`, defined in either `Application` or `Domain` depending on service).
- Select action enum based on operation (`Add`, `Update`, `Remove`).
- Build a clear, structured message including key business context.
- For updates, include original and updated objects when overloads support object diff payloads.
- Publish only for successful business actions; avoid false-positive audit entries on rejected operations.

## Register dependencies

- Register the use case and any new output ports/publishers in the relevant `Add{Feature}Features()` extension.
- Keep registration style consistent with the target service.

## Validate with tests

- Add or update Application tests near the feature.
- Verify success and failure outcomes via output port assertions.
- Verify audit trail publish behavior:
	- Called exactly once for successful audited operations.
	- Not called for rejected/unchanged operations.

## Reusable scaffold

- Start from `assets/golden-template/` for a minimal command use case skeleton.
- Copy and rename symbols/namespaces to the target service and feature.
- Keep the audit trail call and adapt action/message/object diff payloads to the use case.


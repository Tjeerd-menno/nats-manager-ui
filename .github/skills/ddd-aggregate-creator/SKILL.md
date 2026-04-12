---
name: ddd-aggregate-creator
description: Create new DDD aggregates in SmartLab domain services using existing Clean Architecture patterns. Use when adding a new aggregate root and related domain behavior; default to event sourcing (domain events + IEventStoreRepository) unless the user explicitly requests a non-event-sourced aggregate.
---

# DDD Aggregate Creator

Create aggregates by matching the target service's existing style instead of introducing a new one.

## Workflow

1. Identify target service and existing aggregate style.
2. Choose persistence style:
	- Default to event sourcing.
	- Use non-event-sourced aggregate only if explicitly requested.
3. Implement domain aggregate and related domain events/repository contracts.
4. Wire Application and Infrastructure touchpoints.
5. Add or update tests for the new behavior.
6. Build or run focused tests.

## Determine style from target service

Inspect a representative aggregate in the target service before writing code.

- Event-sourced indicators:
  - Aggregate inherits from `AggregateRoot`/`AggregateRoot<TId>` with `When(object @event)`.
  - Mutation methods create events, call `Enqueue(@event)`, then `Apply(@event)`.
  - Event records inherit from `DomainEvent<TId>` and expose `Create(...)` factory.
  - Application use cases depend on `IEventStoreRepository<TAggregate, TId>`.
- Non-event-sourced indicators:
  - Aggregate has direct state mutation without `When(object @event)` replay.
  - Domain layer defines repository interfaces like `I{AggregatePlural}Repository`.

## Event-sourced implementation (default)

Follow this unless the user explicitly requests otherwise.

1. Add aggregate class in Domain:
	- Keep parameterless/private constructor for rehydration.
	- Use static factory (`New` or `Create`) to raise creation event.
	- Implement command methods that enforce invariants and raise events.
	- Implement `Apply(...)` methods and `When(object @event)` switch.
	- Increment `Version` in non-creation `Apply(...)` methods where existing service does so.
2. Add domain event records:
	- Match local placement pattern:
	  - `doses`: event files next to aggregate file.
	  - `speedy-glove-manager`: `DomainEvents/` subfolder.
	- Use `DateTimeOffset.UtcNow` in event factory unless target service uses another established pattern.
3. Update Infrastructure registration:
	- Register `IEventStoreRepository<NewAggregate, TId>` in `InfrastructureServiceCollectionExtension`.
	- If service uses projections/read models for the aggregate, wire projector and query handlers consistently.
4. Update Application use cases to consume `IEventStoreRepository<NewAggregate, TId>`.

For concrete event-sourced checklist and snippet shape, read [references/event-sourced.md](references/event-sourced.md).

## Non-event-sourced implementation (explicit opt-out)

Use only when user explicitly asks for non-event-sourced aggregate.

1. Model aggregate/entity with direct state mutation and invariants.
2. Add/extend Domain repository interface.
3. Implement repository in Infrastructure and register it in DI.
4. Update Application use cases to call the domain repository interface.

For concrete non-event-sourced checklist, read [references/non-event-sourced.md](references/non-event-sourced.md).

## Cross-cutting rules

- Keep Domain free of Infrastructure concerns.
- Keep constructor/factory and naming conventions aligned with the service.
- Use `this.` qualification, explicit types where required, and established namespace/folder layout.
- Do not add new architectural patterns not already present in the target service.
- Prefer small, focused changes and update only required registrations/projections/tests.

# Event-sourced aggregate checklist

Use this checklist when creating a new aggregate in services that already use event sourcing.

## Domain

- Create aggregate class inheriting from local `AggregateRoot` type.
- Add private parameterless constructor for rehydration/deserialization.
- Add private constructor or static factory for creation.
- Raise creation event in factory/constructor with `Enqueue(@event)` + `Apply(@event)`.
- For each behavior method:
  - Validate invariant.
  - Create event (`{Aggregate}{Action}` record).
  - Call `Enqueue(@event)` and `Apply(@event)`.
- Implement `Apply(...)` methods and update state only inside apply methods.
- Implement `When(object @event)` switch to route all event types.
- Increment `Version` in non-creation apply methods when target service does.

## Domain events

- Define event record inheriting `DomainEvent<TId>`.
- Include `Create(...)` factory with `Guid.NewGuid()` event id and `DateTimeOffset.UtcNow` unless service differs.
- Keep event naming consistent with existing service style.
- Place events where the service expects them (same folder or `DomainEvents/`).

## Application

- Inject `IEventStoreRepository<Aggregate, TId>` into relevant use cases.
- Use `Find(id, ct)` + `Add(aggregate, ct)` / `Update(aggregate, cancellationToken: ct)` consistently.
- Keep output-port and presenter flow for business outcomes.

## Infrastructure

- Register `IEventStoreRepository<Aggregate, TId>` in `InfrastructureServiceCollectionExtension`.
- If read models exist for this aggregate, add projector mappings and query handlers.

## Typical aggregate skeleton

```csharp
public sealed class SampleAggregate : AggregateRoot
{
    private SampleAggregate()
    {
    }

    private SampleAggregate(Guid aggregateId, string name)
    {
        SampleCreated @event = SampleCreated.Create(aggregateId, name);
        this.Enqueue(@event);
        this.Apply(@event);
    }

    public string Name { get; private set; } = string.Empty;

    public static SampleAggregate Create(string name) => new(Guid.NewGuid(), name);

    public void Rename(string newName)
    {
        SampleRenamed @event = SampleRenamed.Create(this.Id, newName);
        this.Enqueue(@event);
        this.Apply(@event);
    }

    public override void When(object @event)
    {
        switch (@event)
        {
            case SampleCreated created:
                this.Apply(created);
                return;
            case SampleRenamed renamed:
                this.Apply(renamed);
                return;
        }
    }

    private void Apply(SampleCreated @event)
    {
        this.Id = @event.AggregateId;
        this.Name = @event.Name;
    }

    private void Apply(SampleRenamed @event)
    {
        this.Name = @event.NewName;
        this.Version++;
    }
}
```

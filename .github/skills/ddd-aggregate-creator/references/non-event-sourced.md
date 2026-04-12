# Non-event-sourced aggregate checklist

Use this only when explicitly requested by the user.

## Domain

- Create aggregate/entity class with invariant checks in methods.
- Keep constructor/factory style aligned with service conventions.
- Add or extend domain repository interface for persistence operations.

## Application

- Inject domain repository interface (for example `IIsotopesRepository`).
- Orchestrate business flow through use case and output port.
- Keep validation and presenter flow consistent with existing endpoints.

## Infrastructure

- Implement repository interface in infrastructure persistence class.
- Register repository implementation in `InfrastructureServiceCollectionExtension`.

## Typical shape

```csharp
public interface ISamplesRepository
{
    Task AddSampleAsync(Sample sample);
    Task<Sample?> GetByIdAsync(Guid id);
}

public sealed class Sample : Entity<Guid>, IAggregateRoot
{
    public Sample(Guid id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public override Guid Id { get; protected set; }

    public string Name { get; private set; }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new InvalidOperationException("Name cannot be empty.");
        }

        this.Name = newName;
    }
}
```

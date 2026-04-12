# Application UseCase Patterns

## Pattern checklist

- Match feature folder shape from nearby files.
- Keep interface + implementation signatures consistent.
- Keep output port naming and methods aligned with feature outcomes.
- Keep logging style and message tone aligned with neighboring files.
- Keep `this.` qualification in class members and dependencies.

## Common signature shapes

### Output-port command style

```csharp
public interface IAddThingUseCase
{
    Task Execute(string userName, AddThingInput input, IAddThingOutputPort outputPort, CancellationToken ct);
}
```

### Query style with filter DTO

```csharp
public interface IGetThingsUseCase
{
    Task Execute(GetThingsInput input, IGetThingsOutputPort outputPort, CancellationToken ct);
}
```

Use direct return signatures only when already established in the target feature.

## Dependency patterns seen in repository

- Repository-centric services:
  - Inject domain repositories (`IIsotopesRepository`, etc.) and feature publishers.
- Event-sourced services:
  - Inject `IEventStoreRepository<TAggregate, Guid>`.
  - Inject projection/query delegates (`Func<GetX, CancellationToken, Task<IReadOnlyList<XDbEntity>>>`) where read models are needed.

## Output-port behavior

- Represent expected business failures via output port methods.
- Return early after output-port failure callbacks.
- Trigger success callback only after successful persistence.

## Audit trail integration pattern

Use audited output port available in the target service (often `IAuditTrailOutputPort`).

### Required auditing

- Publish audit entries for every use case action that changes configuration state.
- Publish audit entries for use cases that access sensitive or PII data, including read access.

### Typical publish calls

Without diff:

```csharp
await this.auditTrailOutputPort.PublishAuditTrail(
    userName,
    IAuditTrailOutputPort.PublishAction.Add,
    message);
```

With original/updated payloads:

```csharp
await this.auditTrailOutputPort.PublishAuditTrail(
    userName,
    IAuditTrailOutputPort.PublishAction.Update,
    message,
    originalObject,
    updatedObject);
```

### Audit message guidance

- Include business entity and action.
- Include non-ambiguous identifiers or names.
- Avoid logging secret values directly; include only safe metadata needed for traceability.

## Discovery commands (optional while implementing)

Use these searches to anchor to local conventions before coding:

- `**/*UseCase.cs`
- `PublishAuditTrail`
- `I*OutputPort.cs`
- `ApplicationServiceCollectionExtension.cs`

## Done criteria for a new Application use case

- Interface + implementation added in correct feature folder.
- Output port/input/output DTOs added or reused consistently.
- Persistence and domain calls orchestrated in the service's local pattern.
- Audit trail publishing added for required operations.
- DI registration updated.
- Focused tests updated and passing.

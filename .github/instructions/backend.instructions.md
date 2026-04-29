---
description: "Use when writing or modifying C# backend code in src/. Covers Clean Architecture layers, use case patterns, output ports, auditing, and naming conventions."
applyTo: "src/NatsManager.Domain/**,src/NatsManager.Application/**,src/NatsManager.Infrastructure/**,src/NatsManager.Web/**"
---
# Backend C# Instructions

## Layer Rules

| Layer | May Reference | Must NOT Reference |
|-------|--------------|-------------------|
| Domain | Nothing (no project references) | Application, Infrastructure, Web |
| Application | Domain | Infrastructure, Web |
| Infrastructure | Domain, Application | Web |
| Web | All layers | — |

## Use Case Pattern

Every command/query is an `IUseCase<TRequest, TResult>`. Results flow through `IOutputPort<T>`, never as return values or exceptions. Endpoint presenters and test `TestOutputPort<T>` implementations both rely on the same outcome contract.

```csharp
public sealed class MyCommandHandler(IMyPort port, IAuditTrail auditTrail)
    : IUseCase<MyCommand, Unit>
{
    public async Task ExecuteAsync(
        MyCommand request, IOutputPort<Unit> outputPort, CancellationToken ct)
    {
        // Business logic...
        await auditTrail.RecordAsync(request, ct);
        outputPort.Success(Unit.Value);
    }
}
```

### Output Port Outcomes

Use `outputPort.Success(value)`, `outputPort.NotFound(type, id)`, `outputPort.Conflict(msg)`, or `outputPort.Unauthorized(msg)`. Never throw exceptions for business outcomes.

### Auditable Commands

Commands that change state must implement `IAuditableCommand` and call `auditTrail.RecordAsync(...)` on the success path only.

## Validators

Use FluentValidation. Place validators in the same file as the command/query. They are auto-discovered and wired via `ValidatedUseCase<,>` decorator.

## Domain Aggregates

- Factory methods (`Create(...)`) for construction with invariant enforcement
- Private setters — state mutations via public methods only
- `sealed class` for all implementations
- File-scoped namespaces

## Endpoints (Web Layer)

Minimal API endpoints use `Presenter<T>` to adapt `IOutputPort<T>` to HTTP results:

```csharp
private static async Task<IResult> GetItem(
    Guid id, IUseCase<GetItemQuery, ItemDto> useCase, CancellationToken ct)
{
    var presenter = new Presenter<ItemDto>();
    await useCase.ExecuteAsync(new GetItemQuery(id), presenter, ct);
    return presenter.ToResult();
}
```

## Naming Conventions

- Namespaces: `NatsManager.{Layer}.Modules.{Module}` (e.g., `NatsManager.Application.Modules.Environments.Commands`)
- Suffixes: `Handler`, `Repository`, `Adapter`, `Service`, `Validator`
- Interfaces: `I{Name}` prefix
- Use `sealed` on all non-abstract implementation classes
- Use primary constructors for DI

## Code Style

- File-scoped namespaces (`namespace X;`)
- `var` is acceptable everywhere
- Expression-bodied members preferred for simple methods
- Implicit usings and nullable enabled (via Directory.Build.props)
- Warnings treated as errors — all code must compile warning-free

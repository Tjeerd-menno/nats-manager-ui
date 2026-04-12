# Application UseCase Golden Template

Use this folder as a starting scaffold, then rename symbols and namespaces to the target service/feature.

## Included files

- `IExecuteTemplateUseCase.cs`
- `IExecuteTemplateOutputPort.cs`
- `ExecuteTemplateUseCase.cs`
- `ExecuteTemplateUseCaseTests.cs`

## Adaptation checklist

1. Rename `TemplateService`, `TemplateAggregate`, and `ExecuteTemplate*` symbols.
2. Replace repository contract with service-local abstraction (`IEventStoreRepository<,>` or repository interface).
3. Keep output-port style aligned with nearby use cases.
4. Keep `CancellationToken` usage aligned with local conventions.
5. Keep audit publishing for:
   - configuration-state changes
   - sensitive/PII access (including reads)
6. Update DI registration in the target Application extension.
7. Move tests to the target service test project and align naming conventions.

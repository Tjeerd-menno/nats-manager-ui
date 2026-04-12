# Microsoft Testing Platform v2 Command Recipes

Use these commands for this repo's xUnit v3 + MTP v2 setup.

## Focused local run (single project)

Use during development while iterating on one test project:

```powershell
dotnet test --project path\to\SmartLab.SomeService.Application.Tests.csproj
```

## CI-style unit test run (all unit test executables)

Build first, then run test executables via `--test-modules`:

```powershell
dotnet build SmartLab.Platform.slnx --configuration Release --no-restore
dotnet test --test-modules **\bin\Release\**\*.Tests.exe --coverage --coverage-output-format cobertura --report-trx
```

This matches `.azuredevops/azure-pipelines.yml`.

## CI-style integration test run (specific executable)

```powershell
dotnet test --test-modules **\bin\Release\**\SmartLab.Platform.Core.IntegrationTests.exe --coverage --coverage-output-format cobertura --report-trx
```

## Notes

- Prefer `--test-modules` for broad/CI runs in this monorepo.
- Keep PowerShell glob paths in Windows style for consistency with pipeline scripts.
- Keep package references versionless in test csproj files; versions are centrally managed.

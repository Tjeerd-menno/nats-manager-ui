---
name: backend-unit-test-creator
description: Create or update backend unit tests in SmartLab services using repository conventions, including xUnit v3 with Microsoft Testing Platform v2, AutoFixture + NSubstitute patterns, and correct dotnet test invocation. Use when adding tests for Domain/Application/Infrastructure/Api backend code or creating a new test project in this monorepo.
---

# Backend Unit Test Creator

Create tests by matching the target service's existing style.

## Workflow

1. Identify the target service and layer (`Domain`, `Application`, `Infrastructure`, or `Api`).
2. Inspect nearby tests in the same service/layer and mirror file/folder style.
3. Add or update tests using the repository patterns in [references/unit-test-patterns.md](references/unit-test-patterns.md).
4. If a new test project is required, configure it with the repo's xUnit v3 + MTP v2 setup.
5. Run focused tests first, then broader test commands using [references/mtp-v2-commands.md](references/mtp-v2-commands.md).

## Create or update test projects

When adding a new backend test project:

- Name it `SmartLab.{Service}.{Layer}.Tests`.
- Place it under the service's `test/` or `tests/` folder, matching that service.
- Add versionless package references only (versions come from `Directory.Packages.props`):
	- `xunit.v3.mtp-v2`
	- `Microsoft.Testing.Extensions.CodeCoverage`
	- `Microsoft.Testing.Extensions.TrxReport`
	- `NSubstitute`
	- `AutoFixture`
	- `AutoFixture.AutoNSubstitute`
	- `Shouldly`
- Reference the production project under test.
- Rely on `Directory.Build.Tests.props` for:
	- `TestingPlatformDotnetTestSupport=true`
	- `UseMicrosoftTestingPlatformRunner=true`

## Author test code

- Use explicit `Arrange`, `Act`, `Assert` sections.
- Use `this.` qualification consistently.
- Prefer explicit types; use `var` only when type is obvious from `new`.
- Use `Fixture().Customize(new AutoNSubstituteCustomization())` for application/infrastructure tests that need substitutes.
- Use NSubstitute patterns (`Freeze<T>`, `Returns`, `Received`, `DidNotReceive`, `Arg.Is`).
- Keep test method names in `{Action}_{condition_description}` format.
- Use `[Fact]` for xUnit tests unless nearby tests in that project use another attribute.

## Run tests with xUnit v3 + Microsoft Testing Platform v2

Use command patterns from [references/mtp-v2-commands.md](references/mtp-v2-commands.md).

Critical rule: for CI-style broad test runs in this repo, use `dotnet test --test-modules ...` against built test executables, not legacy VSTest-oriented patterns.


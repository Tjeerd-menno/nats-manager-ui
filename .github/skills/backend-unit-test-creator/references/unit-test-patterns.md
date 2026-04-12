# Unit Test Patterns

Use these patterns to keep backend tests consistent with this monorepo.

## Project and file organization

- Mirror the source structure inside the test project.
- Keep test files close to the feature area (`Isotopes/`, `Geometries/`, `ReferenceSources/`, etc.).
- Use class name `{SutTypeName}Tests`.

## Preferred structure

```csharp
public class AddIsotopeUseCaseTests
{
    private readonly IFixture fixture;

    public AddIsotopeUseCaseTests()
    {
        this.fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
    }

    [Fact]
    public async Task Add_succeeded_when_isotope_does_not_exist()
    {
        // Arrange
        IAddIsotopeOutputPort outputPort = this.fixture.Freeze<IAddIsotopeOutputPort>();
        IIsotopesRepository repository = this.fixture.Freeze<IIsotopesRepository>();
        AddIsotopeUseCase sut = this.fixture.Create<AddIsotopeUseCase>();

        // Act
        await sut.Execute("test-user", input, outputPort);

        // Assert
        await repository.Received(1).AddIsotopeAsync(Arg.Any<Isotope>());
        outputPort.Received(1).CommandAddSuccessfullyHandled();
    }
}
```

## Assertions and substitutes

- Prefer interaction-based assertions for use cases and orchestrators.
- Use `Received(1)` for expected calls.
- Use `DidNotReceive()` for negative paths.
- Use `Arg.Is<T>(...)` to validate object content.

## API and integration-style backend tests

- Use `WebApplicationFactory<Program>` with `IClassFixture<WebApplicationFactory<Program>>`.
- Override authentication in test host when endpoint authorization is present.
- Pass `TestContext.Current.CancellationToken` for HTTP calls that accept cancellation.

## Architecture tests

- Use ArchUnitNET where a service already has architecture tests.
- Keep assertions aligned with existing layer rules in that service.

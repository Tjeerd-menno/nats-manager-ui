---
description: "Use when writing or modifying backend tests (xUnit, NSubstitute, Shouldly). Covers test project conventions, TestOutputPort, mock patterns, and test commands."
applyTo: "tests/NatsManager.Application.Tests/**,tests/NatsManager.Domain.Tests/**,tests/NatsManager.Infrastructure.Tests/**,tests/NatsManager.Web.Tests/**"
---
# Backend Test Instructions

## Framework Stack

- **xUnit v3** with **Microsoft Testing Platform v2** (MTP) — attribute-based: `[Fact]`, `[Theory]`
- **Shouldly** — `.ShouldBe(...)`, `.ShouldBeTrue()`, `.ShouldHaveCount(...)`, `Should.Throw<T>(act)`
- **NSubstitute** — `Substitute.For<IMyPort>()`, `.Returns(...)`, `.Received(...)`
- Centrally managed versions in `Directory.Packages.props`

## Test Project Mapping

| Test Project | Tests For | Pattern |
|---|---|---|
| `Application.Tests` | Use case handlers | Mock ports, assert via `TestOutputPort<T>` |
| `Domain.Tests` | Aggregate invariants | Direct construction, assert state |
| `Infrastructure.Tests` | Repositories, adapters | In-memory SQLite or mocked clients |
| `Web.Tests` | Endpoints (HTTP) | `WebApplicationFactory` + NSubstitute adapters |

## Use Case Handler Tests (Application.Tests)

Use `TestOutputPort<T>` to capture handler output:

```csharp
[Fact]
public async Task Handle_ValidRequest_ReturnsSuccess()
{
    // Arrange
    var repo = Substitute.For<IMyRepository>();
    var auditTrail = Substitute.For<IAuditTrail>();
    var handler = new MyCommandHandler(repo, auditTrail);
    var command = new MyCommand { Name = "test" };

    repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
        .Returns(someEntity);

    // Act
    var outputPort = new TestOutputPort<Unit>();
    await handler.ExecuteAsync(command, outputPort, CancellationToken.None);

    // Assert
    outputPort.IsSuccess.Should().BeTrue();
    await repo.Received(1).SaveAsync(Arg.Any<MyEntity>(), Arg.Any<CancellationToken>());
}
```

### Asserting Business Outcomes

```csharp
outputPort.IsSuccess.Should().BeTrue();       // Happy path
outputPort.IsNotFound.Should().BeTrue();      // Resource not found
outputPort.IsConflict.Should().BeTrue();      // Duplicate/conflict
outputPort.IsUnauthorized.Should().BeTrue();  // Permission denied
outputPort.Value!.Name.Should().Be("test");   // Access result value
```

### Auditable Commands

If the handler takes `IAuditTrail`, add a mock: `Substitute.For<IAuditTrail>()` and pass it to the constructor: `new MyHandler(repo, auditTrail)`.

## Web Endpoint Tests (Web.Tests)

Use `NatsManagerWebAppFactory` with in-memory SQLite and mocked NATS adapters:

```csharp
public sealed class MyEndpointTests(NatsManagerWebAppFactory factory)
    : IClassFixture<NatsManagerWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Get_ShouldReturn200()
    {
        factory.MyAdapter.GetAsync(...).Returns(...);
        var response = await _client.GetAsync("/api/...");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Running Tests

```bash
dotnet test                                              # All backend tests
dotnet test tests/NatsManager.Application.Tests          # Specific project
dotnet test --filter "FullyQualifiedName~Environment"    # Filter by name
```

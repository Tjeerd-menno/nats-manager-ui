using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Services.Commands;
using NatsManager.Application.Modules.Services.Models;
using NatsManager.Application.Modules.Services.Ports;
using NatsManager.Application.Modules.Services.Queries;

namespace NatsManager.Application.Tests.Modules.Services;

public sealed class GetServicesQueryTests
{
    private readonly IServiceDiscoveryAdapter _adapter = Substitute.For<IServiceDiscoveryAdapter>();
    private readonly GetServicesQueryHandler _handler;

    public GetServicesQueryTests()
    {
        _handler = new GetServicesQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnServicesFromAdapter()
    {
        var envId = Guid.NewGuid();
        var services = new List<ServiceInfo>
        {
            new("orders-api", "id1", "1.0.0", "Order service",
                [new ServiceEndpoint("process", "orders.process", null)],
                new ServiceStats(100, 2, TimeSpan.FromSeconds(10), DateTimeOffset.UtcNow))
        };

        _adapter.DiscoverServicesAsync(envId, Arg.Any<CancellationToken>()).Returns(services);

        var outputPort = new TestOutputPort<IReadOnlyList<ServiceInfo>>();
        await _handler.ExecuteAsync(new GetServicesQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Name.Should().Be("orders-api");
    }
}

public sealed class GetServiceDetailQueryTests
{
    private readonly IServiceDiscoveryAdapter _adapter = Substitute.For<IServiceDiscoveryAdapter>();
    private readonly GetServiceDetailQueryHandler _handler;

    public GetServiceDetailQueryTests()
    {
        _handler = new GetServiceDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingService_ShouldReturn()
    {
        var envId = Guid.NewGuid();
        var service = new ServiceInfo("orders-api", "id1", "1.0.0", "Order service", [], null);

        _adapter.GetServiceAsync(envId, "orders-api", Arg.Any<CancellationToken>()).Returns(service);

        var outputPort = new TestOutputPort<ServiceInfo>();
        await _handler.ExecuteAsync(new GetServiceDetailQuery(envId, "orders-api"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value!.Name.Should().Be("orders-api");
    }
}

public sealed class TestServiceRequestCommandTests
{
    private readonly IServiceDiscoveryAdapter _adapter = Substitute.For<IServiceDiscoveryAdapter>();
    private readonly TestServiceRequestCommandHandler _handler;

    public TestServiceRequestCommandTests()
    {
        _handler = new TestServiceRequestCommandHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new TestServiceRequestCommand
        {
            EnvironmentId = envId,
            Subject = "orders.process",
            Payload = "{\"id\":1}"
        };

        _adapter.TestServiceRequestAsync(envId, "orders.process", "{\"id\":1}", Arg.Any<CancellationToken>())
            .Returns("response-data");

        var outputPort = new TestOutputPort<string>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().Be("response-data");
    }
}

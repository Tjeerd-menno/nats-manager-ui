using Shouldly;
using NSubstitute;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Environments.Queries;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Tests.Modules.Environments;

public sealed class GetEnvironmentsQueryTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly GetEnvironmentsQueryHandler _handler;

    public GetEnvironmentsQueryTests()
    {
        _handler = new GetEnvironmentsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedResult()
    {
        var envs = new List<Environment>
        {
            Environment.Create("Env1", "nats://host1:4222"),
            Environment.Create("Env2", "nats://host2:4222")
        };

        _repository.GetPagedAsync(1, 25, null, null, false, Arg.Any<CancellationToken>())
            .Returns((envs.AsReadOnly(), 2));

        var query = new GetEnvironmentsQuery { Page = 1, PageSize = 25 };
        var outputPort = new TestOutputPort<PaginatedResult<EnvironmentListItem>>();
        await _handler.ExecuteAsync(query, outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Items.Count().ShouldBe(2);
        outputPort.Value!.TotalCount.ShouldBe(2);
        outputPort.Value!.Page.ShouldBe(1);
        outputPort.Value!.PageSize.ShouldBe(25);
    }

    [Fact]
    public async Task Handle_ShouldMapPropertiesCorrectly()
    {
        var env = Environment.Create("Test Env", "nats://host:4222", description: "Desc", isProduction: true);
        env.UpdateConnectionStatus(ConnectionStatus.Available);

        _repository.GetPagedAsync(1, 25, null, null, false, Arg.Any<CancellationToken>())
            .Returns((new List<Environment> { env }.AsReadOnly(), 1));

        var outputPort = new TestOutputPort<PaginatedResult<EnvironmentListItem>>();
        await _handler.ExecuteAsync(new GetEnvironmentsQuery(), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        var item = outputPort.Value!.Items.Single();
        item.Name.ShouldBe("Test Env");
        item.Description.ShouldBe("Desc");
        item.IsProduction.ShouldBeTrue();
        item.IsEnabled.ShouldBeTrue();
        item.ConnectionStatus.ShouldBe("Available");
        item.LastSuccessfulContact.ShouldNotBeNull();
    }
}

public sealed class GetEnvironmentDetailQueryTests
{
    private readonly IEnvironmentRepository _repository = Substitute.For<IEnvironmentRepository>();
    private readonly GetEnvironmentDetailQueryHandler _handler;

    public GetEnvironmentDetailQueryTests()
    {
        _handler = new GetEnvironmentDetailQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldReturnDetail()
    {
        var envId = Guid.NewGuid();
        var env = Environment.Create("Test", "nats://host:4222", description: "Desc");

        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns(env);

        var outputPort = new TestOutputPort<EnvironmentDetailResult>();
        await _handler.ExecuteAsync(new GetEnvironmentDetailQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.ShouldBeTrue();
        outputPort.Value!.Name.ShouldBe("Test");
        outputPort.Value!.ServerUrl.ShouldBe("nats://host:4222");
        outputPort.Value!.Description.ShouldBe("Desc");
    }

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldBeNotFound()
    {
        var envId = Guid.NewGuid();
        _repository.GetByIdAsync(envId, Arg.Any<CancellationToken>()).Returns((Environment?)null);

        var outputPort = new TestOutputPort<EnvironmentDetailResult>();
        await _handler.ExecuteAsync(new GetEnvironmentDetailQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsNotFound.ShouldBeTrue();
    }
}

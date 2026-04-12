using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class CoreNatsAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private CoreNatsAdapter CreateAdapter() => new(ConnectionFactory, NullLogger<CoreNatsAdapter>());

    [Fact]
    public async Task GetServerInfoAsync_ShouldReturnServerInfo()
    {
        var adapter = CreateAdapter();

        var info = await adapter.GetServerInfoAsync(EnvironmentId);

        info.Should().NotBeNull();
        info!.Version.Should().NotBeNullOrEmpty();
        info.Port.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotThrow()
    {
        var adapter = CreateAdapter();

        var act = () => adapter.PublishAsync(EnvironmentId, "test.subject", "hello"u8.ToArray());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListSubjectsAsync_ShouldReturnList()
    {
        var adapter = CreateAdapter();

        var subjects = await adapter.ListSubjectsAsync(EnvironmentId);

        subjects.Should().NotBeNull();
    }

    [Fact]
    public async Task ListClientsAsync_ShouldReturnList()
    {
        var adapter = CreateAdapter();

        var clients = await adapter.ListClientsAsync(EnvironmentId);

        clients.Should().NotBeNull();
    }
}

using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class NatsConnectionFactoryTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private NatsConnectionFactory CreateSut(bool isEnabled = true)
        => new(new StaticEnvironmentConnectionResolver(new EnvironmentConnectionInfo(
            NatsUrl,
            "Integration Test",
            isEnabled,
            CredentialType.None)),
            NullLogger<NatsConnectionFactory>());

    [Fact]
    public async Task GetConnectionAsync_ShouldReturnOpenConnection()
    {
        await using var connectionFactory = CreateSut();

        var connection = await connectionFactory.GetConnectionAsync(EnvironmentId);

        connection.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetConnectionAsync_CalledTwice_ShouldReturnSameConnection()
    {
        await using var connectionFactory = CreateSut();

        var first = await connectionFactory.GetConnectionAsync(EnvironmentId);
        var second = await connectionFactory.GetConnectionAsync(EnvironmentId);

        second.ShouldBeSameAs(first);
    }

    [Fact]
    public async Task TestConnectionAsync_WithValidUrl_ShouldReturnAvailable()
    {
        await using var connectionFactory = CreateSut();

        var status = await connectionFactory.TestConnectionAsync(NatsUrl, null);

        status.ShouldBe(ConnectionStatus.Available);
    }

    [Fact]
    public async Task GetConnectionAsync_WhenCalledConcurrently_ShouldReuseSingleConnection()
    {
        await using var connectionFactory = CreateSut();

        var connections = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => connectionFactory.GetConnectionAsync(EnvironmentId)));

        connections.All(connection => ReferenceEquals(connection, connections[0])).ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveConnectionAsync_ShouldDisposeCachedConnectionAndCreateNewOneOnNextRequest()
    {
        await using var connectionFactory = CreateSut();

        var first = await connectionFactory.GetConnectionAsync(EnvironmentId);
        await connectionFactory.RemoveConnectionAsync(EnvironmentId);
        var second = await connectionFactory.GetConnectionAsync(EnvironmentId);

        second.ShouldNotBeSameAs(first);
    }
}

file sealed class StaticEnvironmentConnectionResolver(EnvironmentConnectionInfo connectionInfo) : IEnvironmentConnectionResolver
{
    public Task<EnvironmentConnectionInfo> ResolveAsync(Guid environmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(connectionInfo);
}

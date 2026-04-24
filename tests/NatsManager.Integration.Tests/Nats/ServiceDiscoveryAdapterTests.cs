using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class ServiceDiscoveryAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private ServiceDiscoveryAdapter CreateAdapter() => new(ConnectionFactory, NullLogger<ServiceDiscoveryAdapter>());

    [Fact]
    public async Task DiscoverServicesAsync_WithNoServices_ShouldReturnEmptyList()
    {
        var adapter = CreateAdapter();

        var services = await adapter.DiscoverServicesAsync(EnvironmentId);

        services.ShouldNotBeNull();
        services.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetServiceAsync_WithNonExistentService_ShouldReturnNull()
    {
        var adapter = CreateAdapter();

        var service = await adapter.GetServiceAsync(EnvironmentId, "nonexistent-service");

        service.ShouldBeNull();
    }
}

using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class NatsHealthCheckerTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private static readonly ICredentialEncryptionService NoOpEncryption = new NoOpCredentialEncryptionService();

    [Fact]
    public async Task CheckHealthAsync_WithReachableServer_ShouldReturnSuccess()
    {
        var checker = new NatsHealthChecker(NoOpEncryption, NullLogger<NatsHealthChecker>());

        var result = await checker.CheckHealthAsync(NatsUrl, null);

        result.Reachable.Should().BeTrue();
        result.LatencyMs.Should().BeGreaterThan(0);
        result.ServerVersion.Should().NotBeNullOrEmpty();
        result.JetStreamAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnreachableServer_ShouldReturnFailure()
    {
        var checker = new NatsHealthChecker(NoOpEncryption, NullLogger<NatsHealthChecker>());

        var result = await checker.CheckHealthAsync("nats://nonexistent-host:4222", null);

        result.Reachable.Should().BeFalse();
        result.LatencyMs.Should().BeNull();
        result.ServerVersion.Should().BeNull();
    }

    private sealed class NoOpCredentialEncryptionService : ICredentialEncryptionService
    {
        public string Encrypt(string plainText) => plainText;
        public string Decrypt(string cipherText) => cipherText;
    }
}

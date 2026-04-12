using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NatsManager.Integration.Tests.Infrastructure;

/// <summary>
/// Base class for NATS integration tests. Provides a connection factory
/// and a stable environment ID for all adapter calls.
/// </summary>
public abstract class NatsIntegrationTestBase : IAsyncDisposable
{
    protected static readonly Guid EnvironmentId = Guid.NewGuid();

    protected string NatsUrl { get; }
    protected TestNatsConnectionFactory ConnectionFactory { get; }

    protected NatsIntegrationTestBase(NatsFixture fixture)
    {
        NatsUrl = fixture.NatsUrl;
        ConnectionFactory = new TestNatsConnectionFactory(fixture.NatsUrl);
    }

    protected static ILogger<T> NullLogger<T>()
        => new NullLoggerFactory().CreateLogger<T>();

    public async ValueTask DisposeAsync()
    {
        await ConnectionFactory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

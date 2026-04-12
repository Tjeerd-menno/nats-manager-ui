using NATS.Client.Core;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Integration.Tests.Infrastructure;

/// <summary>
/// Test connection factory that connects directly to a NATS URL,
/// bypassing the environment resolver used in production.
/// </summary>
public sealed class TestNatsConnectionFactory(string natsUrl) : INatsConnectionFactory, IAsyncDisposable
{
    private NatsConnection? _connection;

    public async Task<object> GetConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        if (_connection is null or { ConnectionState: NatsConnectionState.Closed })
        {
            _connection = new NatsConnection(new NatsOpts
            {
                Url = natsUrl,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                Name = "NatsManager-IntegrationTest"
            });
            await _connection.ConnectAsync();
        }

        return _connection;
    }

    public Task<ConnectionStatus> TestConnectionAsync(string serverUrl, string? credentialReference, CancellationToken cancellationToken = default)
        => Task.FromResult(ConnectionStatus.Available);

    public async Task RemoveConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}

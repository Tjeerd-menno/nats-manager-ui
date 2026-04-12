using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NatsManager.Domain.Modules.Common;
using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class NatsConnectionFactory(
    IEnvironmentConnectionResolver connectionResolver,
    ILogger<NatsConnectionFactory> logger) : INatsConnectionFactory, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, NatsConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _connectionLocks = new();
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

    public async Task<object> GetConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        if (TryGetOpenConnection(environmentId, out var existing))
        {
            return existing;
        }

        var connectionLock = _connectionLocks.GetOrAdd(environmentId, _ => new SemaphoreSlim(1, 1));
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (TryGetOpenConnection(environmentId, out existing))
            {
                return existing;
            }

            var environment = await connectionResolver.ResolveAsync(environmentId, cancellationToken);

            if (!environment.IsEnabled)
            {
                throw new InvalidOperationException($"Environment '{environment.Name}' is disabled");
            }

            var opts = new NatsOpts
            {
                Url = environment.ServerUrl,
                ConnectTimeout = ConnectionTimeout,
                Name = $"NatsManager-{environment.Name}",
                AuthOpts = NatsAuthHelper.BuildAuthOpts(environment.CredentialType, environment.Credential)
            };

            var connection = new NatsConnection(opts);
            await connection.ConnectAsync();

            _connections[environmentId] = connection;
            LogConnectionEstablished(environment.Name, environment.ServerUrl);
            return connection;
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async Task<ConnectionStatus> TestConnectionAsync(string serverUrl, string? credentialReference, CancellationToken cancellationToken = default)
    {
        try
        {
            var opts = new NatsOpts
            {
                Url = serverUrl,
                ConnectTimeout = ConnectionTimeout,
                Name = "NatsManager-Test"
            };

            await using var connection = new NatsConnection(opts);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ConnectionTimeout);
            await connection.ConnectAsync();

            return connection.ConnectionState == NatsConnectionState.Open
                ? ConnectionStatus.Available
                : ConnectionStatus.Degraded;
        }
        catch (Exception ex)
        {
            LogConnectionTestFailed(serverUrl, ex.Message);
            return ConnectionStatus.Unavailable;
        }
    }

    public async Task RemoveConnectionAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var connectionLock = _connectionLocks.GetOrAdd(environmentId, _ => new SemaphoreSlim(1, 1));
        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connections.TryRemove(environmentId, out var connection))
            {
                await connection.DisposeAsync();
            }
        }
        finally
        {
            connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _connections)
        {
            await kvp.Value.DisposeAsync();
        }

        _connections.Clear();

        foreach (var connectionLock in _connectionLocks.Values)
        {
            connectionLock.Dispose();
        }

        _connectionLocks.Clear();
    }

    private bool TryGetOpenConnection(Guid environmentId, out NatsConnection connection)
    {
        if (_connections.TryGetValue(environmentId, out connection!) && connection.ConnectionState != NatsConnectionState.Closed)
        {
            return true;
        }

        connection = null!;
        return false;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "NATS connection established to {Name} at {Url}")]
    private partial void LogConnectionEstablished(string name, string url);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NATS connection test failed for {Url}: {Error}")]
    private partial void LogConnectionTestFailed(string url, string error);
}

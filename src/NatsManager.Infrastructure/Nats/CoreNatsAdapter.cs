using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class CoreNatsAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<CoreNatsAdapter> logger) : ICoreNatsAdapter
{
    public async Task<NatsServerInfo?> GetServerInfoAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);

            // Try $SYS.REQ.SERVER.PING for detailed stats (requires system account)
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(3));

                var reply = await connection.RequestAsync<string, string>("$SYS.REQ.SERVER.PING", "", cancellationToken: cts.Token);

                if (reply.Data is not null)
                {
                    var parsed = ParseServerInfo(reply.Data);
                    if (parsed is not null)
                        return parsed;
                }
            }
            catch
            {
                // $SYS request failed (auth, timeout, no responders) — fall through to connection info
            }

            // Fallback: use the connection's built-in server info
            var si = connection.ServerInfo;
            if (si is not null)
            {
                return new NatsServerInfo(
                    ServerId: si.Id ?? "",
                    ServerName: si.Name ?? "",
                    Version: si.Version ?? "",
                    Host: si.Host ?? "",
                    Port: si.Port,
                    MaxPayload: si.MaxPayload,
                    Connections: 0,
                    InMsgs: 0,
                    OutMsgs: 0,
                    InBytes: 0,
                    OutBytes: 0,
                    Uptime: TimeSpan.Zero,
                    JetStreamEnabled: si.JetStreamAvailable);
            }
        }
        catch (Exception ex)
        {
            LogServerInfoError(environmentId, ex);
        }

        return null;
    }

    public Task<IReadOnlyList<NatsSubjectInfo>> ListSubjectsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        // Subject listing via $SYS subjects requires monitoring permissions
        // Return empty for now — can be enhanced with NATS monitoring API
        return Task.FromResult<IReadOnlyList<NatsSubjectInfo>>([]);
    }

    public Task<IReadOnlyList<NatsClientInfo>> ListClientsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        // Client listing via $SYS subjects requires monitoring permissions
        // Return empty for now — can be enhanced with NATS monitoring API
        return Task.FromResult<IReadOnlyList<NatsClientInfo>>([]);
    }

    public async Task PublishAsync(Guid environmentId, string subject, byte[] data, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        await connection.PublishAsync(subject, data, cancellationToken: cancellationToken);
        LogMessagePublished(subject, environmentId);
    }

    private static NatsServerInfo? ParseServerInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var data = root.TryGetProperty("data", out var d) ? d : root;
            var server = data.TryGetProperty("server", out var s) ? s : data;
            var stats = data.TryGetProperty("statsz", out var st) ? st : server;

            return new NatsServerInfo(
                ServerId: server.TryGetProperty("server_id", out var sid) ? sid.GetString() ?? ""
                        : server.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                ServerName: server.TryGetProperty("server_name", out var sn) ? sn.GetString() ?? ""
                          : server.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Version: server.TryGetProperty("version", out var v) ? v.GetString() ?? ""
                       : server.TryGetProperty("ver", out var vr) ? vr.GetString() ?? "" : "",
                Host: server.TryGetProperty("host", out var h) ? h.GetString() ?? "" : "",
                Port: server.TryGetProperty("port", out var p) ? p.GetInt32() : 0,
                MaxPayload: server.TryGetProperty("max_payload", out var mp) ? mp.GetInt32() : 0,
                Connections: stats.TryGetProperty("connections", out var c) ? c.GetInt32() : 0,
                InMsgs: stats.TryGetProperty("in_msgs", out var im) ? im.GetInt64() : 0,
                OutMsgs: stats.TryGetProperty("out_msgs", out var om) ? om.GetInt64() : 0,
                InBytes: stats.TryGetProperty("in_bytes", out var ib) ? ib.GetInt64() : 0,
                OutBytes: stats.TryGetProperty("out_bytes", out var ob) ? ob.GetInt64() : 0,
                Uptime: TimeSpan.Zero,
                JetStreamEnabled: server.TryGetProperty("jetstream", out var js)
                    && (js.ValueKind == JsonValueKind.True || js.ValueKind == JsonValueKind.Object));
        }
        catch
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to get server info for environment {EnvironmentId}")]
    private partial void LogServerInfoError(Guid environmentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published message to {Subject} in environment {EnvironmentId}")]
    private partial void LogMessagePublished(string subject, Guid environmentId);
}

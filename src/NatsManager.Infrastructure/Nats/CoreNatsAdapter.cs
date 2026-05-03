using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Infrastructure.Configuration;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class CoreNatsAdapter(
    INatsConnectionFactory connectionFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<CoreNatsMonitoringOptions> monitoringOptions,
    ILogger<CoreNatsAdapter> logger) : ICoreNatsAdapter
{
    private readonly CoreNatsMonitoringOptions _monitoring = monitoringOptions.Value;

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

    public async Task<ListSubjectsResult> ListSubjectsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);

            var httpClient = httpClientFactory.CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_monitoring.HttpTimeout);

            var url = $"{BuildMonitoringBaseUrl(connection)}/subsz?subs=1";
            var json = await httpClient.GetStringAsync(url, cts.Token);

            using var doc = JsonDocument.Parse(json);
            if (!TryGetSubscriptionsList(doc.RootElement, out var subslist)
                || subslist.ValueKind != JsonValueKind.Array)
            {
                return new ListSubjectsResult([], IsMonitoringAvailable: true);
            }

            var groups = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in subslist.EnumerateArray())
            {
                if (!entry.TryGetProperty("subject", out var subjectProp))
                    continue;
                var subject = subjectProp.GetString() ?? string.Empty;
                if (CoreNatsSubjectFilter.IsInternalSubject(subject))
                    continue;
                groups[subject] = groups.GetValueOrDefault(subject) + 1;
            }

            var subjects = groups
                .Select(kvp => new NatsSubjectInfo(kvp.Key, kvp.Value))
                .OrderBy(s => s.Subject, StringComparer.Ordinal)
                .ToList();

            return new ListSubjectsResult(subjects, IsMonitoringAvailable: true);
        }
        catch (Exception ex)
        {
            LogSubjectsUnavailable(environmentId, ex);
            return new ListSubjectsResult([], IsMonitoringAvailable: false);
        }
    }

    private static bool TryGetSubscriptionsList(JsonElement root, out JsonElement subscriptions) =>
        root.TryGetProperty("subscriptions_list", out subscriptions)
        || root.TryGetProperty("subscriptions", out subscriptions)
        || root.TryGetProperty("subslist", out subscriptions);

    public async Task<IReadOnlyList<NatsClientInfo>> ListClientsAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
            var httpClient = httpClientFactory.CreateClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_monitoring.HttpTimeout);

            var json = await httpClient.GetStringAsync($"{BuildMonitoringBaseUrl(connection)}/connz", cts.Token);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("connections", out var connections)
                || connections.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return [.. connections
                .EnumerateArray()
                .Select(ParseClientInfo)
                .OrderBy(client => client.Id)];
        }
        catch (Exception ex)
        {
            LogClientsUnavailable(environmentId, ex);
            return [];
        }
    }

    private string BuildMonitoringBaseUrl(NatsConnection connection) =>
        !string.IsNullOrEmpty(_monitoring.BaseUrl)
            ? _monitoring.BaseUrl.TrimEnd('/')
            : $"http://{ExtractHost(connection)}:{_monitoring.DefaultPort}";

    private static NatsClientInfo ParseClientInfo(JsonElement client)
    {
        var id = GetInt64(client, "cid", "id");
        var name = GetString(client, "name") ?? $"unnamed-client-{id}";
        string? account = GetString(client, "account", "acc");
        var ip = GetString(client, "ip") ?? string.Empty;
        var port = GetInt32(client, "port");
        var uptimeSeconds = NatsMonitoringUptimeParser.ParseSeconds(GetString(client, "uptime"));

        return new NatsClientInfo(
            Id: id,
            Name: name,
            Account: account,
            Ip: ip,
            Port: port,
            InMsgs: GetInt64(client, "in_msgs"),
            OutMsgs: GetInt64(client, "out_msgs"),
            InBytes: GetInt64(client, "in_bytes"),
            OutBytes: GetInt64(client, "out_bytes"),
            Uptime: TimeSpan.FromSeconds(uptimeSeconds));
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static int GetInt32(JsonElement element, params string[] names) =>
        (int)GetInt64(element, names);

    private static long GetInt64(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }
        }

        return 0;
    }

    public async Task PublishAsync(Guid environmentId, string subject, byte[] data,
        IReadOnlyDictionary<string, string>? headers = null,
        string? replyTo = null,
        CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);

        NatsHeaders? natsHeaders = null;
        if (headers is { Count: > 0 })
        {
            natsHeaders = [];
            foreach (var (key, value) in headers)
                natsHeaders.Add(key, value);
        }

        await connection.PublishAsync(subject, data, headers: natsHeaders, replyTo: replyTo, cancellationToken: cancellationToken);
        LogMessagePublished(subject, environmentId);
    }

    public async IAsyncEnumerable<NatsLiveMessage> SubscribeAsync(
        Guid environmentId,
        string subject,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);

        await foreach (var msg in connection.SubscribeAsync<byte[]>(subject, cancellationToken: cancellationToken))
        {
            var rawBytes = msg.Data ?? [];
            var payloadBase64 = Convert.ToBase64String(rawBytes);
            var payloadSize = rawBytes.Length;
            var isBinary = !IsValidUtf8(rawBytes);

            var flatHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (msg.Headers is not null)
            {
                foreach (var kvp in msg.Headers)
                    flatHeaders[kvp.Key] = string.Join(", ", (IEnumerable<string>)kvp.Value);
            }

            yield return new NatsLiveMessage(
                Subject: msg.Subject,
                ReceivedAt: DateTimeOffset.UtcNow,
                PayloadBase64: payloadBase64,
                PayloadSize: payloadSize,
                Headers: flatHeaders,
                ReplyTo: msg.ReplyTo,
                IsBinary: isBinary);
        }
    }

    private static string ExtractHost(NatsConnection connection)
    {
        try
        {
            var opts = connection.Opts;
            var uri = new Uri(opts.Url ?? "nats://localhost:4222");
            return uri.Host;
        }
        catch
        {
        }

        var serverInfo = connection.ServerInfo;
        if (serverInfo?.Host is { Length: > 0 } host)
            return host;

        return "localhost";
    }

    private static bool IsValidUtf8(byte[] bytes)
    {
        if (bytes.Length == 0) return true;
        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            _ = utf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "NATS monitoring unavailable for environment {EnvironmentId} — subject list will be empty")]
    private partial void LogSubjectsUnavailable(Guid environmentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NATS monitoring unavailable for environment {EnvironmentId} — client list will be empty")]
    private partial void LogClientsUnavailable(Guid environmentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published message to {Subject} in environment {EnvironmentId}")]
    private partial void LogMessagePublished(string subject, Guid environmentId);
}

internal static class CoreNatsSubjectFilter
{
    internal static bool IsInternalSubject(string subject) =>
        string.IsNullOrEmpty(subject)
        || subject[0] == '$'
        || subject.StartsWith("_INBOX.", StringComparison.Ordinal)
        || subject.Equals("_INBOX", StringComparison.Ordinal);
}

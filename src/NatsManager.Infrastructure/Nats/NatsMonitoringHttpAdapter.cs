using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class NatsMonitoringHttpAdapter(
    IHttpClientFactory httpClientFactory,
    IOptions<MonitoringOptions> options,
    ILogger<NatsMonitoringHttpAdapter> logger) : IMonitoringAdapter
{
    public async Task<MonitoringSnapshot> FetchSnapshotAsync(
        Domain.Modules.Environments.Environment environment,
        MonitoringSnapshot? previous,
        CancellationToken ct)
    {
        var baseUrl = environment.MonitoringUrl!.TrimEnd('/');
        var startTime = DateTimeOffset.UtcNow;
        var client = httpClientFactory.CreateClient("NatsMonitoring");

        NatsVarzResponse? varz = null;
        NatsJszResponse? jsz = null;
        NatsHealthzResponse? healthz = null;

        try
        {
            varz = await client.GetFromJsonAsync<NatsVarzResponse>($"{baseUrl}/varz", ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            LogFetchFailed(baseUrl, ex.Message);
            return CreateUnavailableSnapshot(environment.Id);
        }

        if (varz is null)
            return CreateDegradedSnapshot(environment.Id);

        try
        {
            var jszResponse = await client.GetAsync($"{baseUrl}/jsz", ct);
            if (jszResponse.StatusCode == HttpStatusCode.NotFound)
            {
                LogJetStreamUnavailable(baseUrl);
            }
            else
            {
                jszResponse.EnsureSuccessStatusCode();
                jsz = await jszResponse.Content.ReadFromJsonAsync<NatsJszResponse>(ct);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            LogFetchFailed($"{baseUrl}/jsz", ex.Message);
        }
        catch (Exception)
        {
            // JSON parse error for jsz = degraded; continue with null JetStream
        }

        var healthStatus = MonitoringStatus.Unavailable;
        try
        {
            var healthzResponse = await client.GetAsync($"{baseUrl}/healthz", ct);
            if (healthzResponse.IsSuccessStatusCode)
            {
                healthz = await healthzResponse.Content.ReadFromJsonAsync<NatsHealthzResponse>(ct);
                healthStatus = string.Equals(healthz?.Status, "ok", StringComparison.OrdinalIgnoreCase)
                    ? MonitoringStatus.Ok
                    : MonitoringStatus.Degraded;
            }
            else
            {
                healthStatus = MonitoringStatus.Degraded;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            healthStatus = MonitoringStatus.Unavailable;
            LogFetchFailed($"{baseUrl}/healthz", ex.Message);
        }

        var latencyMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        LogFetchSuccess(baseUrl, latencyMs);

        var intervalSeconds = environment.MonitoringPollingIntervalSeconds ?? options.Value.DefaultPollingIntervalSeconds;
        var server = BuildServerMetrics(varz, previous, intervalSeconds);
        var jetStream = jsz is not null ? BuildJetStreamMetrics(jsz) : null;

        return new MonitoringSnapshot(
            EnvironmentId: environment.Id,
            Timestamp: DateTimeOffset.UtcNow,
            Server: server,
            JetStream: jetStream,
            Status: MonitoringStatus.Ok,
            HealthStatus: healthStatus);
    }

    private static MonitoringSnapshot CreateUnavailableSnapshot(Guid envId) =>
        new(envId, DateTimeOffset.UtcNow,
            new ServerMetrics("", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            null, MonitoringStatus.Unavailable, MonitoringStatus.Unavailable);

    private static MonitoringSnapshot CreateDegradedSnapshot(Guid envId) =>
        new(envId, DateTimeOffset.UtcNow,
            new ServerMetrics("", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            null, MonitoringStatus.Degraded, MonitoringStatus.Unavailable);

    private static ServerMetrics BuildServerMetrics(NatsVarzResponse varz, MonitoringSnapshot? previous, int intervalSeconds)
    {
        double inMsgsPerSec = 0, outMsgsPerSec = 0, inBytesPerSec = 0, outBytesPerSec = 0;
        if (previous?.Server is { } prev && intervalSeconds > 0)
        {
            inMsgsPerSec = (varz.InMsgs - prev.InMsgsTotal) / (double)intervalSeconds;
            outMsgsPerSec = (varz.OutMsgs - prev.OutMsgsTotal) / (double)intervalSeconds;
            inBytesPerSec = (varz.InBytes - prev.InBytesTotal) / (double)intervalSeconds;
            outBytesPerSec = (varz.OutBytes - prev.OutBytesTotal) / (double)intervalSeconds;
        }

        return new ServerMetrics(
            Version: varz.Version ?? "",
            Connections: varz.Connections,
            TotalConnections: varz.TotalConnections,
            MaxConnections: varz.MaxConnections,
            InMsgsTotal: varz.InMsgs,
            OutMsgsTotal: varz.OutMsgs,
            InBytesTotal: varz.InBytes,
            OutBytesTotal: varz.OutBytes,
            InMsgsPerSec: Math.Max(0, inMsgsPerSec),
            OutMsgsPerSec: Math.Max(0, outMsgsPerSec),
            InBytesPerSec: Math.Max(0, inBytesPerSec),
            OutBytesPerSec: Math.Max(0, outBytesPerSec),
            UptimeSeconds: ParseUptimeSeconds(varz.Uptime),
            MemoryBytes: varz.Mem);
    }

    private static JetStreamMetrics BuildJetStreamMetrics(NatsJszResponse jsz) =>
        new(StreamCount: jsz.Streams, ConsumerCount: jsz.Consumers,
            TotalMessages: jsz.Messages, TotalBytes: jsz.Bytes);

    private static long ParseUptimeSeconds(string? uptime)
    {
        if (string.IsNullOrEmpty(uptime)) return 0;
        long total = 0;
        var s = uptime.AsSpan();
        while (!s.IsEmpty)
        {
            var i = 0;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            if (i == 0) break;
            if (!long.TryParse(s[..i], out var val)) break;
            var unit = i < s.Length ? s[i] : ' ';
            total += unit switch
            {
                'd' => val * 86400,
                'h' => val * 3600,
                'm' => val * 60,
                's' => val,
                _ => 0
            };
            s = s[(i + 1)..];
        }
        return total;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Monitoring fetch succeeded for {Url} in {LatencyMs}ms")]
    private partial void LogFetchSuccess(string url, long latencyMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Monitoring fetch failed for {Url}: {Error}")]
    private partial void LogFetchFailed(string url, string error);

    [LoggerMessage(Level = LogLevel.Debug, Message = "JetStream not available at {Url}")]
    private partial void LogJetStreamUnavailable(string url);
}

internal sealed record NatsVarzResponse(
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("connections")] int Connections,
    [property: JsonPropertyName("total_connections")] long TotalConnections,
    [property: JsonPropertyName("max_connections")] int MaxConnections,
    [property: JsonPropertyName("in_msgs")] long InMsgs,
    [property: JsonPropertyName("out_msgs")] long OutMsgs,
    [property: JsonPropertyName("in_bytes")] long InBytes,
    [property: JsonPropertyName("out_bytes")] long OutBytes,
    [property: JsonPropertyName("uptime")] string? Uptime,
    [property: JsonPropertyName("mem")] long Mem);

internal sealed record NatsJszResponse(
    [property: JsonPropertyName("streams")] int Streams,
    [property: JsonPropertyName("consumers")] int Consumers,
    [property: JsonPropertyName("messages")] long Messages,
    [property: JsonPropertyName("bytes")] long Bytes);

internal sealed record NatsHealthzResponse(
    [property: JsonPropertyName("status")] string? Status);

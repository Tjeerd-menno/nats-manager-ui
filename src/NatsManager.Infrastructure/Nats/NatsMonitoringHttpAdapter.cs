using System.Net;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NatsManager.Application.Modules.Monitoring.Models;
using NatsManager.Application.Modules.Monitoring.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class NatsMonitoringHttpAdapter(
    IHttpClientFactory httpClientFactory,
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

        var varzResult = await client.GetJsonWithHandlingAsync<NatsVarzResponse>($"{baseUrl}/varz", ct);
        if (!varzResult.IsSuccess)
        {
            LogFetchFailed($"{baseUrl}/varz", varzResult.ErrorMessage ?? "Unknown error");
            return varzResult.FailureKind == MonitoringFailureKind.Json
                ? NatsMonitoringStateFactory.CreateSnapshot(environment.Id, MonitoringStatus.Degraded, MonitoringStatus.Unavailable)
                : NatsMonitoringStateFactory.CreateSnapshot(environment.Id, MonitoringStatus.Unavailable, MonitoringStatus.Unavailable);
        }

        var varz = varzResult.Value;
        if (varz is null)
        {
            return NatsMonitoringStateFactory.CreateSnapshot(environment.Id, MonitoringStatus.Degraded, MonitoringStatus.Unavailable);
        }

        var jszResult = await client.GetJsonWithHandlingAsync<NatsJszResponse>(
            $"{baseUrl}/jsz",
            ct,
            async (response, cancellationToken) =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    LogJetStreamUnavailable(baseUrl);
                    return MonitoringHttpResult<NatsJszResponse>.Success(null);
                }

                response.EnsureSuccessStatusCode();
                return await response.ReadJsonOrFailureAsync<NatsJszResponse>(cancellationToken);
            });

        var degraded = jszResult.FailureKind != MonitoringFailureKind.None || jszResult.Value is null;
        if (jszResult.FailureKind != MonitoringFailureKind.None)
        {
            LogFetchFailed($"{baseUrl}/jsz", jszResult.ErrorMessage ?? "Unknown error");
        }

        var healthzResult = await client.GetJsonWithHandlingAsync<NatsHealthzResponse>(
            $"{baseUrl}/healthz",
            ct,
            async (response, cancellationToken) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return MonitoringHttpResult<NatsHealthzResponse>.Success(new NatsHealthzResponse("degraded"));
                }

                return await response.ReadJsonOrFailureAsync<NatsHealthzResponse>(cancellationToken);
            });

        var healthStatus = healthzResult.FailureKind switch
        {
            MonitoringFailureKind.None => string.Equals(healthzResult.Value?.Status, "ok", StringComparison.OrdinalIgnoreCase)
                ? MonitoringStatus.Ok
                : MonitoringStatus.Degraded,
            MonitoringFailureKind.Timeout or MonitoringFailureKind.HttpRequest => MonitoringStatus.Unavailable,
            _ => MonitoringStatus.Degraded
        };

        if (healthzResult.FailureKind != MonitoringFailureKind.None)
        {
            LogFetchFailed($"{baseUrl}/healthz", healthzResult.ErrorMessage ?? "Unknown error");
        }

        var latencyMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
        LogFetchSuccess(baseUrl, latencyMs);

        var timestamp = DateTimeOffset.UtcNow;
        degraded = degraded || healthStatus != MonitoringStatus.Ok;
        var server = BuildServerMetrics(varz, previous, timestamp);
        var jetStream = jszResult.Value is not null ? BuildJetStreamMetrics(jszResult.Value) : null;

        return new MonitoringSnapshot(
            EnvironmentId: environment.Id,
            Timestamp: timestamp,
            Server: server,
            JetStream: jetStream,
            Status: degraded ? MonitoringStatus.Degraded : MonitoringStatus.Ok,
            HealthStatus: healthStatus);
    }

    private static ServerMetrics BuildServerMetrics(NatsVarzResponse varz, MonitoringSnapshot? previous, DateTimeOffset timestamp)
    {
        double inMsgsPerSec = 0, outMsgsPerSec = 0, inBytesPerSec = 0, outBytesPerSec = 0;
        if (previous is { Status: not MonitoringStatus.Unavailable, Server: { } prev })
        {
            var elapsedSeconds = (timestamp - previous.Timestamp).TotalSeconds;
            if (elapsedSeconds > 0)
            {
                inMsgsPerSec = (varz.InMsgs - prev.InMsgsTotal) / elapsedSeconds;
                outMsgsPerSec = (varz.OutMsgs - prev.OutMsgsTotal) / elapsedSeconds;
                inBytesPerSec = (varz.InBytes - prev.InBytesTotal) / elapsedSeconds;
                outBytesPerSec = (varz.OutBytes - prev.OutBytesTotal) / elapsedSeconds;
            }
        }

        return new ServerMetrics(
            Version: varz.Version ?? string.Empty,
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
            UptimeSeconds: NatsMonitoringUptimeParser.ParseSeconds(varz.Uptime),
            MemoryBytes: varz.Mem);
    }

    private static JetStreamMetrics BuildJetStreamMetrics(NatsJszResponse jsz) =>
        new(StreamCount: jsz.Streams, ConsumerCount: jsz.Consumers,
            TotalMessages: jsz.Messages, TotalBytes: jsz.Bytes);

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

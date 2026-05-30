using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Infrastructure.Nats.ClusterObservability;

public sealed partial class NatsClusterMonitoringHttpAdapter(
    IHttpClientFactory httpClientFactory,
    IEnvironmentRepository environmentRepository,
    IOptions<MonitoringOptions> options,
    ILogger<NatsClusterMonitoringHttpAdapter> logger) : IClusterMonitoringAdapter
{
    public async Task<ClusterObservation> GetClusterObservationAsync(Guid environmentId, CancellationToken ct)
    {
        var environment = await environmentRepository.GetByIdAsync(environmentId, ct);
        if (environment?.MonitoringUrl is null)
        {
            return NatsMonitoringStateFactory.CreateUnavailableClusterObservation(environmentId);
        }

        var baseUrl = environment.MonitoringUrl.TrimEnd('/');
        var observedAt = DateTimeOffset.UtcNow;

        var healthzTask = SafeFetchAsync(() => FetchHealthzAsync(baseUrl, ct), MonitoringEndpoint.Healthz);
        var varzTask = SafeFetchAsync(() => FetchVarzAsync(baseUrl, ct), MonitoringEndpoint.Varz);
        var jszTask = SafeFetchAsync(() => FetchJszAsync(baseUrl, ct), MonitoringEndpoint.Jsz);
        var routezTask = SafeFetchAsync(() => FetchRoutezAsync(baseUrl, ct), MonitoringEndpoint.Routez);
        var gatewayzTask = SafeFetchAsync(() => FetchGatewayzAsync(baseUrl, ct), MonitoringEndpoint.Gatewayz);
        var leafzTask = SafeFetchAsync(() => FetchLeafzAsync(baseUrl, ct), MonitoringEndpoint.Leafz);

        await Task.WhenAll(healthzTask, varzTask, jszTask, routezTask, gatewayzTask, leafzTask);

        var varz = varzTask.Result;
        var jsz = jszTask.Result;
        var routez = routezTask.Result;
        var gatewayz = gatewayzTask.Result;
        var leafz = leafzTask.Result;

        if (varz is null)
        {
            LogEndpointFailed(MonitoringEndpoint.Varz.ToString(), "Primary monitoring endpoint unavailable.");
            return NatsMonitoringStateFactory.CreateUnavailableClusterObservation(environmentId);
        }

        var server = NatsClusterObservationMapper.BuildServerObservation(environmentId, varz, observedAt);
        var topology = NatsClusterObservationMapper.BuildTopologyRelationships(environmentId, routez, gatewayz, leafz, observedAt);

        var servers = new List<ServerObservation> { server };
        var warnings = NatsClusterObservationMapper.DeriveWarnings(servers, options.Value);
        var clusterStatus = ClusterHealthDerivation.DeriveClusterStatus(servers);
        var freshness = ClusterHealthDerivation.DeriveFreshness(servers);

        return new ClusterObservation(
            EnvironmentId: environmentId,
            ObservedAt: observedAt,
            Status: clusterStatus,
            Freshness: freshness,
            ServerCount: servers.Count,
            DegradedServerCount: servers.Count(s => s.Status is ServerStatus.Warning or ServerStatus.Stale or ServerStatus.Unavailable),
            JetStreamAvailable: jsz?.Enabled,
            ConnectionCount: varz.Connections,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            Warnings: warnings,
            Servers: servers,
            Topology: topology);
    }

    private async Task<T?> SafeFetchAsync<T>(Func<Task<MonitoringHttpResult<T>>> fetch, MonitoringEndpoint endpoint)
        where T : class
    {
        var result = await fetch();
        if (!result.IsSuccess)
        {
            LogEndpointFailed(endpoint.ToString(), result.ErrorMessage ?? "Unknown error");
        }

        return result.Value;
    }

    public async Task<ClusterHealthzResponse?> GetHealthzAsync(string baseUrl, CancellationToken ct)
        => (await FetchHealthzAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterHealthzResponse>> FetchHealthzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<HealthzRaw>(
            $"{baseUrl}/healthz",
            ct,
            async (response, cancellationToken) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return MonitoringHttpResult<HealthzRaw>.Success(null);
                }

                return await response.ReadJsonOrFailureAsync<HealthzRaw>(cancellationToken);
            });

        return rawResult.FailureKind == MonitoringFailureKind.None
            ? MonitoringHttpResult<ClusterHealthzResponse>.Success(rawResult.Value is null ? null : new ClusterHealthzResponse(rawResult.Value.Status ?? "unknown", null))
            : MonitoringHttpResult<ClusterHealthzResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
    }

    public async Task<ClusterVarzResponse?> GetVarzAsync(string baseUrl, CancellationToken ct)
        => (await FetchVarzAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterVarzResponse>> FetchVarzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<VarzRaw>($"{baseUrl}/varz", ct);
        return rawResult.FailureKind == MonitoringFailureKind.None
            ? MonitoringHttpResult<ClusterVarzResponse>.Success(rawResult.Value is null ? null : new ClusterVarzResponse(
                ServerId: rawResult.Value.ServerId,
                ServerName: rawResult.Value.ServerName,
                ClusterName: NatsClusterObservationMapper.ExtractClusterName(rawResult.Value.Cluster),
                Version: rawResult.Value.Version,
                Uptime: rawResult.Value.Uptime,
                UptimeSeconds: NatsMonitoringUptimeParser.ParseSeconds(rawResult.Value.Uptime),
                Connections: rawResult.Value.Connections,
                MaxConnections: rawResult.Value.MaxConnections,
                SlowConsumers: rawResult.Value.SlowConsumers,
                InMsgs: rawResult.Value.InMsgs,
                OutMsgs: rawResult.Value.OutMsgs,
                InBytes: rawResult.Value.InBytes,
                OutBytes: rawResult.Value.OutBytes,
                Mem: rawResult.Value.Mem))
            : MonitoringHttpResult<ClusterVarzResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
    }

    public async Task<ClusterJszResponse?> GetJszAsync(string baseUrl, CancellationToken ct)
        => (await FetchJszAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterJszResponse>> FetchJszAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<JszRaw>(
            $"{baseUrl}/jsz",
            ct,
            async (response, cancellationToken) =>
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return MonitoringHttpResult<JszRaw>.Success(null);
                }

                response.EnsureSuccessStatusCode();
                return await response.ReadJsonOrFailureAsync<JszRaw>(cancellationToken);
            });

        if (rawResult.FailureKind != MonitoringFailureKind.None)
        {
            return MonitoringHttpResult<ClusterJszResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
        }

        return MonitoringHttpResult<ClusterJszResponse>.Success(
            rawResult.Value is null ? new ClusterJszResponse(false, 0, 0, 0, 0) : new ClusterJszResponse(true, rawResult.Value.Streams, rawResult.Value.Consumers, rawResult.Value.Messages, rawResult.Value.Bytes));
    }

    public async Task<ClusterRoutezResponse?> GetRoutezAsync(string baseUrl, CancellationToken ct)
        => (await FetchRoutezAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterRoutezResponse>> FetchRoutezAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<RoutezRaw>(
            $"{baseUrl}/routez",
            ct,
            async (response, cancellationToken) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return MonitoringHttpResult<RoutezRaw>.Success(null);
                }

                return await response.ReadJsonOrFailureAsync<RoutezRaw>(cancellationToken);
            });

        if (rawResult.FailureKind != MonitoringFailureKind.None)
        {
            return MonitoringHttpResult<ClusterRoutezResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
        }

        var routes = rawResult.Value?.Routes?.Select(route => new SafeRouteInfo(
            RemoteId: route.RemoteId,
            RemoteName: route.RemoteName,
            Solicited: route.Solicited,
            TlsRequired: route.TlsRequired)).ToList() ?? [];

        return MonitoringHttpResult<ClusterRoutezResponse>.Success(new ClusterRoutezResponse(routes));
    }

    public async Task<ClusterGatewayzResponse?> GetGatewayzAsync(string baseUrl, CancellationToken ct)
        => (await FetchGatewayzAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterGatewayzResponse>> FetchGatewayzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<GatewayzRaw>(
            $"{baseUrl}/gatewayz",
            ct,
            async (response, cancellationToken) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return MonitoringHttpResult<GatewayzRaw>.Success(null);
                }

                return await response.ReadJsonOrFailureAsync<GatewayzRaw>(cancellationToken);
            });

        if (rawResult.FailureKind != MonitoringFailureKind.None)
        {
            return MonitoringHttpResult<ClusterGatewayzResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
        }

        var gateways = rawResult.Value?.OutboundGateways?.Select(gateway => new SafeGatewayInfo(
            Name: gateway.Key,
            IsConfigured: true,
            Status: gateway.Value?.Status)).ToList() ?? [];

        return MonitoringHttpResult<ClusterGatewayzResponse>.Success(new ClusterGatewayzResponse(rawResult.Value?.Name, gateways));
    }

    public async Task<ClusterLeafzResponse?> GetLeafzAsync(string baseUrl, CancellationToken ct)
        => (await FetchLeafzAsync(baseUrl, ct)).Value;

    private async Task<MonitoringHttpResult<ClusterLeafzResponse>> FetchLeafzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var rawResult = await client.GetJsonWithHandlingAsync<LeafzRaw>(
            $"{baseUrl}/leafz",
            ct,
            async (response, cancellationToken) =>
            {
                if (!response.IsSuccessStatusCode)
                {
                    return MonitoringHttpResult<LeafzRaw>.Success(null);
                }

                return await response.ReadJsonOrFailureAsync<LeafzRaw>(cancellationToken);
            });

        if (rawResult.FailureKind != MonitoringFailureKind.None)
        {
            return MonitoringHttpResult<ClusterLeafzResponse>.Failure(rawResult.FailureKind, rawResult.ErrorMessage ?? "Unknown error");
        }

        var leafs = rawResult.Value?.Leafs?.Select(leaf => new SafeLeafInfo(
            Name: leaf.Name,
            RemoteUrl: NatsClusterObservationMapper.MaskUrl(leaf.RemoteUrl),
            IsHub: leaf.IsHub,
            InMsgs: leaf.InMsgs,
            OutMsgs: leaf.OutMsgs)).ToList() ?? [];

        return MonitoringHttpResult<ClusterLeafzResponse>.Success(new ClusterLeafzResponse(leafs));
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "NATS cluster monitoring endpoint failed: {Endpoint} — {Reason}")]
    private partial void LogEndpointFailed(string endpoint, string reason);

#pragma warning disable CS8618
    private sealed class HealthzRaw { public string? Status { get; set; } }
    private sealed class VarzRaw
    {
        [JsonPropertyName("server_id")] public string? ServerId { get; set; }
        [JsonPropertyName("server_name")] public string? ServerName { get; set; }
        [JsonPropertyName("cluster")] public JsonElement? Cluster { get; set; }
        [JsonPropertyName("version")] public string? Version { get; set; }
        [JsonPropertyName("uptime")] public string? Uptime { get; set; }
        [JsonPropertyName("connections")] public int Connections { get; set; }
        [JsonPropertyName("max_connections")] public int MaxConnections { get; set; }
        [JsonPropertyName("slow_consumers")] public int SlowConsumers { get; set; }
        [JsonPropertyName("in_msgs")] public long InMsgs { get; set; }
        [JsonPropertyName("out_msgs")] public long OutMsgs { get; set; }
        [JsonPropertyName("in_bytes")] public long InBytes { get; set; }
        [JsonPropertyName("out_bytes")] public long OutBytes { get; set; }
        [JsonPropertyName("mem")] public long Mem { get; set; }
    }
    private sealed class JszRaw
    {
        [JsonPropertyName("streams")] public int Streams { get; set; }
        [JsonPropertyName("consumers")] public int Consumers { get; set; }
        [JsonPropertyName("messages")] public long Messages { get; set; }
        [JsonPropertyName("bytes")] public long Bytes { get; set; }
    }
    private sealed class RoutezRaw
    {
        [JsonPropertyName("routes")] public List<RouteRaw>? Routes { get; set; }
    }
    private sealed class RouteRaw
    {
        [JsonPropertyName("remote_id")] public string? RemoteId { get; set; }
        [JsonPropertyName("name")] public string? RemoteName { get; set; }
        [JsonPropertyName("solicited")] public bool Solicited { get; set; }
        [JsonPropertyName("tls_required")] public bool TlsRequired { get; set; }
    }
    private sealed class GatewayzRaw
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("outbound_gateways")] public Dictionary<string, GatewayInfoRaw?>? OutboundGateways { get; set; }
    }
    private sealed class GatewayInfoRaw { [JsonPropertyName("status")] public string? Status { get; set; } }
    private sealed class LeafzRaw { [JsonPropertyName("leafs")] public List<LeafRaw>? Leafs { get; set; } }
    private sealed class LeafRaw
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("remote")] public string? RemoteUrl { get; set; }
        [JsonPropertyName("is_spoke")] public bool IsSpoke { get; set; }
        public bool IsHub => !IsSpoke;
        [JsonPropertyName("in_msgs")] public long InMsgs { get; set; }
        [JsonPropertyName("out_msgs")] public long OutMsgs { get; set; }
    }
#pragma warning restore CS8618
}

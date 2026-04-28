using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Infrastructure.Nats.ClusterObservability;

/// <summary>
/// HTTP adapter that calls NATS monitoring endpoints per environment.
/// Uses a 10s timeout per endpoint. Marks missing endpoints as Unavailable/Partial
/// rather than failing the whole observation. Masks/omits JWT and credential fields.
/// </summary>
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
            return CreateUnavailableObservation(environmentId, "Monitoring URL not configured.");
        }

        var baseUrl = environment.MonitoringUrl.TrimEnd('/');
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");

        var observedAt = DateTimeOffset.UtcNow;

        // Fetch all endpoints in parallel, isolating failures
        var healthzTask = SafeFetchAsync(() => GetHealthzAsync(baseUrl, ct), MonitoringEndpoint.Healthz);
        var varzTask = SafeFetchAsync(() => GetVarzAsync(baseUrl, ct), MonitoringEndpoint.Varz);
        var jszTask = SafeFetchAsync(() => GetJszAsync(baseUrl, ct), MonitoringEndpoint.Jsz);
        var routezTask = SafeFetchAsync(() => GetRoutezAsync(baseUrl, ct), MonitoringEndpoint.Routez);
        var gatewayzTask = SafeFetchAsync(() => GetGatewayzAsync(baseUrl, ct), MonitoringEndpoint.Gatewayz);
        var leafzTask = SafeFetchAsync(() => GetLeafzAsync(baseUrl, ct), MonitoringEndpoint.Leafz);

        await Task.WhenAll(healthzTask, varzTask, jszTask, routezTask, gatewayzTask, leafzTask);

        var varz = varzTask.Result;
        var jsz = jszTask.Result;
        var routez = routezTask.Result;
        var gatewayz = gatewayzTask.Result;
        var leafz = leafzTask.Result;

        if (varz is null)
        {
            LogEndpointFailed(baseUrl, "varz");
            return CreateUnavailableObservation(environmentId, "Primary monitoring endpoint /varz unavailable.");
        }

        var server = BuildServerObservation(environmentId, varz, jsz, observedAt);
        var topology = BuildTopologyRelationships(environmentId, routez, gatewayz, leafz, observedAt);

        var servers = new List<ServerObservation> { server };
        var warnings = DeriveWarnings(servers, options.Value);
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

    private async Task<T?> SafeFetchAsync<T>(Func<Task<T?>> fetch, MonitoringEndpoint endpoint) where T : class
    {
        try
        {
            return await fetch();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogEndpointFailed(endpoint.ToString(), ex.Message);
            return null;
        }
    }

    public async Task<ClusterHealthzResponse?> GetHealthzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var response = await client.GetAsync($"{baseUrl}/healthz", ct);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<HealthzRaw>(ct);
        return raw is null ? null : new ClusterHealthzResponse(raw.Status ?? "unknown", null);
    }

    public async Task<ClusterVarzResponse?> GetVarzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var raw = await client.GetFromJsonAsync<VarzRaw>($"{baseUrl}/varz", ct);
        if (raw is null) return null;
        return new ClusterVarzResponse(
            ServerId: raw.ServerId,
            ServerName: raw.ServerName,
            ClusterName: raw.Cluster,
            Version: raw.Version,
            Uptime: raw.Uptime,
            UptimeSeconds: ParseUptimeSeconds(raw.Uptime),
            Connections: raw.Connections,
            MaxConnections: raw.MaxConnections,
            SlowConsumers: raw.SlowConsumers,
            InMsgs: raw.InMsgs,
            OutMsgs: raw.OutMsgs,
            InBytes: raw.InBytes,
            OutBytes: raw.OutBytes,
            Mem: raw.Mem);
    }

    public async Task<ClusterJszResponse?> GetJszAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var response = await client.GetAsync($"{baseUrl}/jsz", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return new ClusterJszResponse(Enabled: false, Streams: 0, Consumers: 0, Messages: 0, Bytes: 0);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadFromJsonAsync<JszRaw>(ct);
        return raw is null ? null : new ClusterJszResponse(
            Enabled: true,
            Streams: raw.Streams,
            Consumers: raw.Consumers,
            Messages: raw.Messages,
            Bytes: raw.Bytes);
    }

    public async Task<ClusterRoutezResponse?> GetRoutezAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var response = await client.GetAsync($"{baseUrl}/routez", ct);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<RoutezRaw>(ct);
        var routes = raw?.Routes?.Select(r => new SafeRouteInfo(
            RemoteId: r.RemoteId,
            RemoteName: r.RemoteName,
            Solicited: r.Solicited,
            TlsRequired: r.TlsRequired)).ToList() ?? [];
        return new ClusterRoutezResponse(routes);
    }

    public async Task<ClusterGatewayzResponse?> GetGatewayzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var response = await client.GetAsync($"{baseUrl}/gatewayz", ct);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<GatewayzRaw>(ct);
        var gateways = raw?.OutboundGateways?.Select(kvp => new SafeGatewayInfo(
            Name: kvp.Key,
            IsConfigured: true,
            Status: kvp.Value?.Status)).ToList() ?? [];
        return new ClusterGatewayzResponse(Name: raw?.Name, Gateways: gateways);
    }

    public async Task<ClusterLeafzResponse?> GetLeafzAsync(string baseUrl, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("NatsClusterMonitoring");
        var response = await client.GetAsync($"{baseUrl}/leafz", ct);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<LeafzRaw>(ct);
        var leafs = raw?.Leafs?.Select(l => new SafeLeafInfo(
            Name: l.Name,
            RemoteUrl: MaskUrl(l.RemoteUrl),
            IsHub: l.IsHub,
            InMsgs: l.InMsgs,
            OutMsgs: l.OutMsgs)).ToList() ?? [];
        return new ClusterLeafzResponse(leafs);
    }

    private static ServerObservation BuildServerObservation(
        Guid environmentId,
        ClusterVarzResponse varz,
        ClusterJszResponse? jsz,
        DateTimeOffset observedAt)
    {
        var status = DeriveServerStatus(varz);
        return new ServerObservation(
            EnvironmentId: environmentId,
            ServerId: varz.ServerId ?? "unknown",
            ServerName: varz.ServerName,
            ClusterName: varz.ClusterName,
            Version: varz.Version,
            UptimeSeconds: varz.UptimeSeconds > 0 ? varz.UptimeSeconds : null,
            Status: status,
            Freshness: ObservationFreshness.Live,
            Connections: varz.Connections,
            MaxConnections: varz.MaxConnections > 0 ? varz.MaxConnections : null,
            SlowConsumers: varz.SlowConsumers,
            MemoryBytes: varz.Mem > 0 ? varz.Mem : null,
            StorageBytes: null,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            InBytesPerSecond: null,
            OutBytesPerSecond: null,
            LastObservedAt: observedAt,
            MetricStates: [MetricState.Live]);
    }

    private static ServerStatus DeriveServerStatus(ClusterVarzResponse varz)
    {
        if (varz.SlowConsumers > 0)
            return ServerStatus.Warning;
        return ServerStatus.Healthy;
    }

    private static List<TopologyRelationship> BuildTopologyRelationships(
        Guid environmentId,
        ClusterRoutezResponse? routez,
        ClusterGatewayzResponse? gatewayz,
        ClusterLeafzResponse? leafz,
        DateTimeOffset observedAt)
    {
        var relationships = new List<TopologyRelationship>();

        if (routez is not null)
        {
            foreach (var route in routez.Routes)
            {
                var targetId = route.RemoteId ?? $"route-{Guid.NewGuid():N}";
                var relId = $"route__{targetId}";
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: relId,
                    SourceNodeId: "local",
                    TargetNodeId: targetId,
                    Type: TopologyRelationshipType.Route,
                    Direction: RelationshipDirection.Bidirectional,
                    Status: RelationshipStatus.Healthy,
                    Freshness: ObservationFreshness.Live,
                    ObservedAt: observedAt,
                    SourceEndpoint: MonitoringEndpoint.Routez,
                    SafeLabel: route.RemoteName ?? targetId));
            }
        }

        if (gatewayz is not null)
        {
            foreach (var gw in gatewayz.Gateways)
            {
                var gwId = $"gateway-{gw.Name ?? Guid.NewGuid().ToString("N")}";
                var relId = $"gateway__{gwId}";
                var status = gw.Status is "CONNECTED" ? RelationshipStatus.Healthy : RelationshipStatus.Warning;
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: relId,
                    SourceNodeId: "local",
                    TargetNodeId: gwId,
                    Type: TopologyRelationshipType.Gateway,
                    Direction: RelationshipDirection.Outbound,
                    Status: status,
                    Freshness: ObservationFreshness.Live,
                    ObservedAt: observedAt,
                    SourceEndpoint: MonitoringEndpoint.Gatewayz,
                    SafeLabel: $"gateway: {gw.Name ?? "unknown"}"));
            }
        }

        if (leafz is not null)
        {
            foreach (var leaf in leafz.Leafs)
            {
                var leafId = $"leaf-{leaf.Name ?? Guid.NewGuid().ToString("N")}";
                var relId = $"leaf__{leafId}";
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: relId,
                    SourceNodeId: "local",
                    TargetNodeId: leafId,
                    Type: TopologyRelationshipType.LeafNode,
                    Direction: leaf.IsHub ? RelationshipDirection.Inbound : RelationshipDirection.Outbound,
                    Status: RelationshipStatus.Healthy,
                    Freshness: ObservationFreshness.Live,
                    ObservedAt: observedAt,
                    SourceEndpoint: MonitoringEndpoint.Leafz,
                    SafeLabel: $"leaf: {leaf.Name ?? "unknown"}"));
            }
        }

        return relationships;
    }

    private static List<ClusterWarning> DeriveWarnings(
        IReadOnlyList<ServerObservation> servers,
        MonitoringOptions opts)
    {
        var warnings = new List<ClusterWarning>();
        foreach (var s in servers)
        {
            if (s.SlowConsumers >= opts.SlowConsumerWarningThreshold)
            {
                warnings.Add(new ClusterWarning(
                    Code: "SlowConsumers",
                    Severity: "Warning",
                    Message: $"{s.ServerId} has {s.SlowConsumers} slow consumer(s)",
                    ServerId: s.ServerId));
            }

            if (s.Freshness == ObservationFreshness.Stale)
            {
                warnings.Add(new ClusterWarning(
                    Code: "StaleServer",
                    Severity: "Warning",
                    Message: $"{s.ServerId} has not refreshed within the configured freshness window",
                    ServerId: s.ServerId));
            }

            if (s.Connections.HasValue && s.MaxConnections.HasValue && s.MaxConnections.Value > 0)
            {
                var pressure = (s.Connections.Value * 100) / s.MaxConnections.Value;
                if (pressure >= opts.ConnectionPressureWarningPercent)
                {
                    warnings.Add(new ClusterWarning(
                        Code: "ConnectionPressure",
                        Severity: "Warning",
                        Message: $"{s.ServerId} connection pressure at {pressure}%",
                        ServerId: s.ServerId));
                }
            }
        }
        return warnings;
    }

    private static ClusterObservation CreateUnavailableObservation(Guid environmentId, string reason) =>
        new(
            EnvironmentId: environmentId,
            ObservedAt: DateTimeOffset.UtcNow,
            Status: ClusterStatus.Unavailable,
            Freshness: ObservationFreshness.Unavailable,
            ServerCount: 0,
            DegradedServerCount: 0,
            JetStreamAvailable: null,
            ConnectionCount: null,
            InMsgsPerSecond: null,
            OutMsgsPerSecond: null,
            Warnings: [],
            Servers: [],
            Topology: []);

    private static long ParseUptimeSeconds(string? uptime)
    {
        if (string.IsNullOrEmpty(uptime)) return 0;
        // NATS reports uptime like "1d2h3m4s" — simple parse
        long seconds = 0;
        var s = uptime.AsSpan();
        while (!s.IsEmpty)
        {
            var digitEnd = 0;
            while (digitEnd < s.Length && char.IsDigit(s[digitEnd])) digitEnd++;
            if (digitEnd == 0) break;
            if (!long.TryParse(s[..digitEnd], out var num)) break;
            s = s[digitEnd..];
            if (s.IsEmpty) break;
            switch (s[0])
            {
                case 'd': seconds += num * 86400; break;
                case 'h': seconds += num * 3600; break;
                case 'm': seconds += num * 60; break;
                case 's': seconds += num; break;
            }
            s = s[1..];
        }
        return seconds;
    }

    private static string? MaskUrl(string? url)
    {
        if (url is null) return null;
        // Mask credentials in URL if present
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (!string.IsNullOrEmpty(uri.UserInfo))
                return $"{uri.Scheme}://***@{uri.Host}:{uri.Port}{uri.PathAndQuery}";
        }
        return url;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "NATS cluster monitoring endpoint failed: {Endpoint} — {Reason}")]
    private partial void LogEndpointFailed(string endpoint, string reason);

    // Raw JSON shapes — only safe fields mapped; JWT/payload fields intentionally excluded
#pragma warning disable CS8618
    private sealed class HealthzRaw { public string? Status { get; set; } }
    private sealed class VarzRaw
    {
        [JsonPropertyName("server_id")] public string? ServerId { get; set; }
        [JsonPropertyName("server_name")] public string? ServerName { get; set; }
        [JsonPropertyName("cluster")] public string? Cluster { get; set; }
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

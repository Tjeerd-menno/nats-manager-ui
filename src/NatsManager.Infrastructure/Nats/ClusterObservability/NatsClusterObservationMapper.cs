using System.Text.Json;
using NatsManager.Application.Modules.Monitoring;
using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;
using NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

namespace NatsManager.Infrastructure.Nats.ClusterObservability;

/// <summary>
/// Pure projections from raw NATS monitoring responses into cluster observation
/// domain models. Extracted from <see cref="NatsClusterMonitoringHttpAdapter"/> to
/// isolate side-effect-free mapping logic and make it independently testable.
/// </summary>
internal static class NatsClusterObservationMapper
{
    public static string? ExtractClusterName(JsonElement? cluster)
    {
        if (cluster is null)
        {
            return null;
        }

        return cluster.Value.ValueKind switch
        {
            JsonValueKind.String => cluster.Value.GetString(),
            JsonValueKind.Object when cluster.Value.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String => name.GetString(),
            _ => null
        };
    }

    public static ServerObservation BuildServerObservation(Guid environmentId, ClusterVarzResponse varz, DateTimeOffset observedAt) =>
        new(
            EnvironmentId: environmentId,
            ServerId: varz.ServerId ?? "unknown",
            ServerName: varz.ServerName,
            ClusterName: varz.ClusterName,
            Version: varz.Version,
            UptimeSeconds: varz.UptimeSeconds > 0 ? varz.UptimeSeconds : null,
            Status: varz.SlowConsumers > 0 ? ServerStatus.Warning : ServerStatus.Healthy,
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

    public static List<TopologyRelationship> BuildTopologyRelationships(
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
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: $"route__{targetId}",
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
            foreach (var gateway in gatewayz.Gateways)
            {
                var gatewayId = $"gateway-{gateway.Name ?? Guid.NewGuid().ToString("N")}";
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: $"gateway__{gatewayId}",
                    SourceNodeId: "local",
                    TargetNodeId: gatewayId,
                    Type: TopologyRelationshipType.Gateway,
                    Direction: RelationshipDirection.Outbound,
                    Status: gateway.Status is "CONNECTED" ? RelationshipStatus.Healthy : RelationshipStatus.Warning,
                    Freshness: ObservationFreshness.Live,
                    ObservedAt: observedAt,
                    SourceEndpoint: MonitoringEndpoint.Gatewayz,
                    SafeLabel: $"gateway: {gateway.Name ?? "unknown"}"));
            }
        }

        if (leafz is not null)
        {
            foreach (var leaf in leafz.Leafs)
            {
                var leafId = $"leaf-{leaf.Name ?? Guid.NewGuid().ToString("N")}";
                relationships.Add(new TopologyRelationship(
                    EnvironmentId: environmentId,
                    RelationshipId: $"leaf__{leafId}",
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

    public static List<ClusterWarning> DeriveWarnings(IReadOnlyList<ServerObservation> servers, MonitoringOptions opts)
    {
        var warnings = new List<ClusterWarning>();
        foreach (var server in servers)
        {
            if (server.SlowConsumers >= opts.SlowConsumerWarningThreshold)
            {
                warnings.Add(new ClusterWarning("SlowConsumers", "Warning", $"{server.ServerId} has {server.SlowConsumers} slow consumer(s)", server.ServerId));
            }

            if (server.Freshness == ObservationFreshness.Stale)
            {
                warnings.Add(new ClusterWarning("StaleServer", "Warning", $"{server.ServerId} has not refreshed within the configured freshness window", server.ServerId));
            }

            if (server.Connections.HasValue && server.MaxConnections.HasValue && server.MaxConnections.Value > 0)
            {
                var pressure = server.Connections.Value * 100 / server.MaxConnections.Value;
                if (pressure >= opts.ConnectionPressureWarningPercent)
                {
                    warnings.Add(new ClusterWarning("ConnectionPressure", "Warning", $"{server.ServerId} connection pressure at {pressure}%", server.ServerId));
                }
            }
        }

        return warnings;
    }

    public static string? MaskUrl(string? url)
    {
        if (url is null)
        {
            return null;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.UserInfo))
        {
            return $"{uri.Scheme}://***@{uri.Host}:{uri.Port}{uri.PathAndQuery}";
        }

        return url;
    }
}

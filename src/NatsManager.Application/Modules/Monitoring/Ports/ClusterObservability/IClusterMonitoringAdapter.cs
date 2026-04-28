using NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

namespace NatsManager.Application.Modules.Monitoring.Ports.ClusterObservability;

public interface IClusterMonitoringAdapter
{
    Task<ClusterObservation> GetClusterObservationAsync(Guid environmentId, CancellationToken ct);
    Task<ClusterHealthzResponse?> GetHealthzAsync(string baseUrl, CancellationToken ct);
    Task<ClusterVarzResponse?> GetVarzAsync(string baseUrl, CancellationToken ct);
    Task<ClusterJszResponse?> GetJszAsync(string baseUrl, CancellationToken ct);
    Task<ClusterRoutezResponse?> GetRoutezAsync(string baseUrl, CancellationToken ct);
    Task<ClusterGatewayzResponse?> GetGatewayzAsync(string baseUrl, CancellationToken ct);
    Task<ClusterLeafzResponse?> GetLeafzAsync(string baseUrl, CancellationToken ct);
}

// Safe response types — only safe fields, no JWT/payload content
public sealed record ClusterHealthzResponse(string Status, string? StatusCode);
public sealed record ClusterVarzResponse(
    string? ServerId,
    string? ServerName,
    string? ClusterName,
    string? Version,
    string? Uptime,
    long UptimeSeconds,
    int Connections,
    int MaxConnections,
    int SlowConsumers,
    long InMsgs,
    long OutMsgs,
    long InBytes,
    long OutBytes,
    long Mem);
public sealed record ClusterJszResponse(bool Enabled, int Streams, int Consumers, long Messages, long Bytes);
public sealed record ClusterRoutezResponse(IReadOnlyList<SafeRouteInfo> Routes);
public sealed record SafeRouteInfo(string? RemoteId, string? RemoteName, bool Solicited, bool TlsRequired);
public sealed record ClusterGatewayzResponse(string? Name, IReadOnlyList<SafeGatewayInfo> Gateways);
public sealed record SafeGatewayInfo(string? Name, bool IsConfigured, string? Status);
public sealed record ClusterLeafzResponse(IReadOnlyList<SafeLeafInfo> Leafs);
public sealed record SafeLeafInfo(string? Name, string? RemoteUrl, bool IsHub, long InMsgs, long OutMsgs);

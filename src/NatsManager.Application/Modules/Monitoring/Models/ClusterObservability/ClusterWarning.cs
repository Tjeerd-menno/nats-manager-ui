namespace NatsManager.Application.Modules.Monitoring.Models.ClusterObservability;

public sealed record ClusterWarning(
    string Code,
    string Severity,
    string Message,
    string? ServerId = null);

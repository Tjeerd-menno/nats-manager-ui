namespace NatsManager.Application.Modules.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int DefaultPollingIntervalSeconds { get; set; } = 30;
    public int MaxSnapshotsPerEnvironment { get; set; } = 120;
    public int HttpTimeoutSeconds { get; set; } = 10;

    // Cluster Observability options
    public int ClusterPollingIntervalSeconds { get; set; } = 30;
    public int ClusterEndpointTimeoutSeconds { get; set; } = 10;
    public int StaleThresholdSeconds { get; set; } = 90;
    public int MaxRetainedObservations { get; set; } = 10;

    // Server warning thresholds
    public int SlowConsumerWarningThreshold { get; set; } = 1;
    public int ConnectionPressureWarningPercent { get; set; } = 80;
    public int StoragePressureWarningPercent { get; set; } = 80;

    public static bool IsValid(MonitoringOptions options) =>
        options.DefaultPollingIntervalSeconds is >= 5 and <= 300
        && options.MaxSnapshotsPerEnvironment is >= 1 and <= 10_000
        && options.HttpTimeoutSeconds is >= 1 and <= 60
        && options.ClusterPollingIntervalSeconds is >= 5 and <= 300
        && options.ClusterEndpointTimeoutSeconds is >= 1 and <= 10
        && options.StaleThresholdSeconds is >= 10 and <= 600
        && options.MaxRetainedObservations is >= 1 and <= 100;
}

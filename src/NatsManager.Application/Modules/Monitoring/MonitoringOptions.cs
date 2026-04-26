namespace NatsManager.Application.Modules.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int DefaultPollingIntervalSeconds { get; set; } = 30;
    public int MaxSnapshotsPerEnvironment { get; set; } = 120;
    public int HttpTimeoutSeconds { get; set; } = 10;
}

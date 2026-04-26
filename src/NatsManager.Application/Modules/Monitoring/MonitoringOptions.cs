namespace NatsManager.Application.Modules.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";
    public int DefaultPollingIntervalSeconds { get; set; } = 30;
    public int MaxSnapshotsPerEnvironment { get; set; } = 120;
    public int HttpTimeoutSeconds { get; set; } = 10;

    public static bool IsValid(MonitoringOptions options) =>
        options.DefaultPollingIntervalSeconds is >= 5 and <= 300
        && options.MaxSnapshotsPerEnvironment is >= 1 and <= 10_000
        && options.HttpTimeoutSeconds is >= 1 and <= 60;
}

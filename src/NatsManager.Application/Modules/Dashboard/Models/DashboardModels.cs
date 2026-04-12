namespace NatsManager.Application.Modules.Dashboard.Models;

public sealed record DashboardSummary(
    EnvironmentHealth Environment,
    JetStreamSummary JetStream,
    KvSummary KeyValue,
    IReadOnlyList<DashboardAlert> Alerts);

public sealed record EnvironmentHealth(
    string ConnectionStatus,
    DateTimeOffset? LastSuccessfulContact);

public sealed record JetStreamSummary(
    int StreamCount,
    int ConsumerCount,
    int UnhealthyConsumers,
    long TotalMessages,
    long TotalBytes);

public sealed record KvSummary(
    int BucketCount,
    long TotalKeys);

public sealed record DashboardAlert(
    string Severity,
    string ResourceType,
    string ResourceName,
    string Message);

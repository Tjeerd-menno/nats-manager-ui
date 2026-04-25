namespace NatsManager.Infrastructure.Configuration;

public sealed class CoreNatsMonitoringOptions
{
    public const string SectionName = "CoreNats:Monitoring";
    public int DefaultPort { get; set; } = 8222;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(3);
}

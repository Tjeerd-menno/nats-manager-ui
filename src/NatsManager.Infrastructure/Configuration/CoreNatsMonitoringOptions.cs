namespace NatsManager.Infrastructure.Configuration;

public sealed class CoreNatsMonitoringOptions
{
    public const string SectionName = "CoreNats:Monitoring";
    public int DefaultPort { get; set; } = 8222;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Optional base URL for the NATS monitoring HTTP endpoint (e.g. "http://localhost:12345").
    /// When set (e.g. injected by Aspire), this is used instead of constructing the URL from
    /// the NATS connection host and <see cref="DefaultPort"/>.
    /// </summary>
    public string? BaseUrl { get; set; }
}

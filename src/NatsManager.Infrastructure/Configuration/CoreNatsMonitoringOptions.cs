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

    /// <summary>
    /// Validates the bound options. The default values are valid; configuration that supplies an
    /// out-of-range port, a non-positive/excessive timeout, or a base URL that is not a valid
    /// http/https URL fails fast at startup.
    /// </summary>
    public static bool IsValid(CoreNatsMonitoringOptions options) =>
        options.DefaultPort is >= 1 and <= 65535
        && options.HttpTimeout > TimeSpan.Zero
        && options.HttpTimeout <= TimeSpan.FromSeconds(60)
        && (string.IsNullOrWhiteSpace(options.BaseUrl)
            || (Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri)
                && (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps)));
}

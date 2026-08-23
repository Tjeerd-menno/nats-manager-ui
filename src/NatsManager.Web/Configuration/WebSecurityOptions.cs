namespace NatsManager.Web.Configuration;

/// <summary>
/// Opt-out switches for the request-pipeline protections that are awkward to drive from
/// an automated test.
/// <para>
/// All three default to <see langword="true"/>, so a deployment that configures nothing runs
/// fully protected — the protections are never keyed off the hosting environment name.
/// A test that needs one disabled must say so explicitly in configuration, which keeps
/// the opt-out visible at the call site instead of implied by <c>ASPNETCORE_ENVIRONMENT</c>.
/// </para>
/// </summary>
public sealed class WebSecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// When enabled (the default), unsafe <c>/api</c> requests must carry a valid
    /// <c>X-XSRF-TOKEN</c> header and GET requests are issued an <c>XSRF-TOKEN</c> cookie.
    /// </summary>
    public bool EnableAntiforgery { get; init; } = true;

    /// <summary>
    /// When enabled (the default), the rate limiter partitions and throttles requests —
    /// most importantly the login endpoint.
    /// </summary>
    public bool EnableRateLimiting { get; init; } = true;

    /// <summary>
    /// When enabled (the default outside Development), HTTP requests are redirected to
    /// HTTPS and HSTS is advertised.
    /// </summary>
    public bool EnableHttpsRedirection { get; init; } = true;
}

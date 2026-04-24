using FluentValidation;

namespace NatsManager.Application.Modules.Environments.Commands;

/// <summary>
/// Centralised validation for NATS environment server URLs.
/// Limits the scheme to the set of protocols the NATS.Net client understands and
/// rejects obviously malformed URLs or SSRF-prone schemes such as <c>file://</c>,
/// <c>http://</c>, or <c>gopher://</c>.
/// </summary>
internal static class ServerUrlValidation
{
    // NATS.Net v2 supports nats://, tls://, ws://, wss://.
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "nats",
        "tls",
        "ws",
        "wss"
    };

    public static IRuleBuilderOptions<T, string> MustBeValidNatsServerUrl<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(BeValidNatsUrl)
            .WithMessage("ServerUrl must be an absolute URL using one of the allowed schemes: nats://, tls://, ws://, wss://.");

    private static bool BeValidNatsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // NotEmpty rule handles this separately; treat as valid here to avoid
            // doubled-up error messages.
            return true;
        }

        // NATS supports comma-separated seed URLs — validate each segment.
        foreach (var raw in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!AllowedSchemes.Contains(uri.Scheme))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(uri.Host))
            {
                return false;
            }
        }

        return true;
    }
}

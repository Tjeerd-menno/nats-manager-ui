namespace NatsManager.Web.Configuration;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    public bool Enabled { get; init; }
    public string? Authority { get; init; }
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public bool RequireHttpsMetadata { get; init; } = true;
    public string CallbackPath { get; init; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";
    public string? PublicOrigin { get; init; }
    public string? PostLogoutRedirectUri { get; init; }
    public List<string> AllowedRedirectOrigins { get; init; } = [];
    public List<string> Scopes { get; init; } = ["openid", "profile", "email"];
    public List<string> DefaultRoles { get; init; } = [];
    public Dictionary<string, string> RoleMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(OidcOptions options)
    {
        if (!options.Enabled)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Authority)
            && Uri.TryCreate(options.Authority, UriKind.Absolute, out _)
            && !string.IsNullOrWhiteSpace(options.ClientId)
            && options.Scopes.Any(scope => string.Equals(scope, "openid", StringComparison.Ordinal));
    }
}

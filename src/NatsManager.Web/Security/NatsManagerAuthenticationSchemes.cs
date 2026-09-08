namespace NatsManager.Web.Security;

public static class NatsManagerAuthenticationSchemes
{
    public const string Composite = "NatsManager";
    public const string OidcCookie = "NatsManagerOidcCookie";
    public const string OidcCookieName = ".NatsManager.Oidc";
}

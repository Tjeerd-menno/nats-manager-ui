namespace NatsManager.Web.Security;

/// <summary>
/// Named rate-limiter policies applied to specific endpoints.
/// </summary>
public static class RateLimitPolicyNames
{
    /// <summary>Strict per-IP throttle on authentication attempts.</summary>
    public const string Login = "login";
}

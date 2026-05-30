namespace NatsManager.Infrastructure.Configuration;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string DisplayName { get; init; } = "Administrator";

    /// <summary>
    /// Validates the bound options without requiring credentials to be present (an existing database
    /// may run without bootstrap configuration). When a password is supplied it must be at least
    /// 8 characters, and the display name must not be blank.
    /// </summary>
    public static bool IsValid(BootstrapAdminOptions options) =>
        !string.IsNullOrWhiteSpace(options.DisplayName)
        && (string.IsNullOrEmpty(options.Password) || options.Password.Length >= 8);
}

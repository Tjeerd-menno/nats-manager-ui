namespace NatsManager.Infrastructure.Configuration;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "BootstrapAdmin";

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string DisplayName { get; init; } = "Administrator";
}

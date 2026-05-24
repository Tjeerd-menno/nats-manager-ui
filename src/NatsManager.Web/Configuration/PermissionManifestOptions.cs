using System.Security.Cryptography;
using System.Text;

namespace NatsManager.Web.Configuration;

public sealed class PermissionManifestOptions
{
    public const string SectionName = "PermissionManifest";

    public PermissionManifestExposureMode ExposureMode { get; init; }

    public string? RestrictedAccessKeyHash { get; init; }

    public string ApplicationId { get; init; } = string.Empty;

    public string ApplicationName { get; init; } = string.Empty;

    public string? ApplicationVersion { get; init; }

    public static bool IsValid(PermissionManifestOptions options)
    {
        if (options.ExposureMode is not (PermissionManifestExposureMode.Public or PermissionManifestExposureMode.Restricted))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.ApplicationId)
            || string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            return false;
        }

        if (options.ApplicationVersion is not null
            && string.IsNullOrWhiteSpace(options.ApplicationVersion))
        {
            return false;
        }

        if (options.ExposureMode == PermissionManifestExposureMode.Restricted)
        {
            return IsSha256Hash(options.RestrictedAccessKeyHash);
        }

        return true;
    }

    public static string HashAccessKey(string accessKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(accessKey));
        return Convert.ToBase64String(hash);
    }

    private static bool IsSha256Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value).Length == 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public enum PermissionManifestExposureMode
{
    Unspecified,
    Public,
    Restricted
}

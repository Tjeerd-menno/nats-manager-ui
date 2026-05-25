namespace NatsManager.Web.Configuration;

public sealed class ObjectStoreUploadOptions
{
    public const string SectionName = "ObjectStore";
    public const long DefaultMaxUploadBytes = 64L * 1024 * 1024;
    public const long DefaultMaxDownloadBytes = DefaultMaxUploadBytes;

    public long MaxUploadBytes { get; init; } = DefaultMaxUploadBytes;
    public long MaxDownloadBytes { get; init; } = DefaultMaxDownloadBytes;

    public static bool IsValid(ObjectStoreUploadOptions options) =>
        options.MaxUploadBytes is > 0 and <= int.MaxValue
        && options.MaxDownloadBytes is > 0 and <= int.MaxValue;
}

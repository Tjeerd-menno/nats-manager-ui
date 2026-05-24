namespace NatsManager.Application.Modules.Permissions.Models;

public sealed record PermissionManifest(
    ApplicationMetadata Application,
    IReadOnlyList<PermissionDefinition> Permissions);

public sealed record ApplicationMetadata(
    string Id,
    string Name,
    string? Version = null);

public sealed record PermissionDefinition(
    string Name,
    string Description,
    string? Category = null);

public enum PermissionManifestPublicationStatus
{
    Valid,
    InvalidWithFallback,
    InvalidNoFallback
}

public enum PermissionManifestSource
{
    Current,
    LastValid
}

public sealed record PermissionManifestPublicationState(
    PermissionManifest? LastValidManifest,
    DateTimeOffset? LastValidatedAt,
    string? LastFailureReason,
    PermissionManifestPublicationStatus Status);

public sealed record ManifestRetrievalResult(
    PermissionManifest? Manifest,
    PermissionManifestSource? Source,
    string? FailureReason)
{
    public bool IsSuccess => Manifest is not null;

    public static ManifestRetrievalResult Current(PermissionManifest manifest) =>
        new(manifest, PermissionManifestSource.Current, null);

    public static ManifestRetrievalResult LastValid(PermissionManifest manifest, string? failureReason) =>
        new(manifest, PermissionManifestSource.LastValid, failureReason);

    public static ManifestRetrievalResult Failure(string failureReason) =>
        new(null, null, failureReason);
}

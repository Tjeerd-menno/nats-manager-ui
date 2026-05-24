using NatsManager.Application.Modules.Permissions.Models;

namespace NatsManager.Application.Modules.Permissions.Services;

public interface IPermissionManifestValidator
{
    PermissionManifestValidationResult Validate(PermissionManifest? manifest);
}

public sealed record PermissionManifestValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors)
{
    public static PermissionManifestValidationResult Success { get; } = new(true, []);

    public static PermissionManifestValidationResult Failure(IReadOnlyList<string> errors) =>
        new(false, errors);
}

public sealed class PermissionManifestValidator : IPermissionManifestValidator
{
    public PermissionManifestValidationResult Validate(PermissionManifest? manifest)
    {
        var errors = new List<string>();

        if (manifest is null)
        {
            errors.Add("Permission manifest is required.");
            return PermissionManifestValidationResult.Failure(errors);
        }

        ValidateApplication(manifest.Application, errors);
        ValidatePermissions(manifest.Permissions, errors);

        return errors.Count == 0
            ? PermissionManifestValidationResult.Success
            : PermissionManifestValidationResult.Failure(errors);
    }

    private static void ValidateApplication(ApplicationMetadata application, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(application.Id))
        {
            errors.Add("Application id is required.");
        }

        if (string.IsNullOrWhiteSpace(application.Name))
        {
            errors.Add("Application name is required.");
        }

        if (application.Version is not null && string.IsNullOrWhiteSpace(application.Version))
        {
            errors.Add("Application version must be non-empty when provided.");
        }
    }

    private static void ValidatePermissions(IReadOnlyList<PermissionDefinition> permissions, List<string> errors)
    {
        var permissionNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permission in permissions)
        {
            var trimmedName = permission.Name?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                errors.Add("Permission name is required.");
                continue;
            }

            if (!permissionNames.Add(trimmedName))
            {
                errors.Add($"Duplicate permission name '{trimmedName}'.");
            }

            if (string.IsNullOrWhiteSpace(permission.Description))
            {
                errors.Add($"Permission description is required for '{trimmedName}'.");
            }

            if (permission.Category is not null && string.IsNullOrWhiteSpace(permission.Category))
            {
                errors.Add($"Permission category must be non-empty when provided for '{trimmedName}'.");
            }
        }
    }
}

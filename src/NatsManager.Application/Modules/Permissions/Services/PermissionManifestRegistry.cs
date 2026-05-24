using NatsManager.Application.Modules.Permissions.Models;

namespace NatsManager.Application.Modules.Permissions.Services;

public interface IPermissionManifestRegistry
{
    PermissionManifest CreateManifest(ApplicationMetadata application);
}

public sealed record PermissionManifestRegistryEntry(
    string Name,
    string Description,
    string? Category = null,
    bool IsActive = true);

public sealed class PermissionManifestRegistry : IPermissionManifestRegistry
{
    private readonly IReadOnlyList<PermissionManifestRegistryEntry> entries;

    public PermissionManifestRegistry(IEnumerable<PermissionManifestRegistryEntry> entries)
    {
        this.entries = entries.ToArray();
    }

    public static PermissionManifestRegistry CreateDefault() =>
        new(DefaultEntries);

    public PermissionManifest CreateManifest(ApplicationMetadata application)
    {
        var activePermissions = entries
            .Where(entry => entry.IsActive)
            .Select(entry => new PermissionDefinition(
                entry.Name.Trim(),
                entry.Description.Trim(),
                NormalizeOptional(entry.Category)))
            .ToArray();

        return new PermissionManifest(application, activePermissions);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly IReadOnlyList<PermissionManifestRegistryEntry> DefaultEntries =
    [
        new("read:environments", "Allows reading registered NATS environments", "Environments"),
        new("write:environments", "Allows creating or modifying registered NATS environments", "Environments"),
        new("read:streams", "Allows reading JetStream stream definitions and status", "JetStream"),
        new("write:streams", "Allows creating, updating, or deleting JetStream streams", "JetStream"),
        new("read:consumers", "Allows reading JetStream consumer definitions and status", "JetStream"),
        new("write:consumers", "Allows creating, updating, or deleting JetStream consumers", "JetStream"),
        new("read:key-value", "Allows reading Key Value bucket metadata and entries", "Key Value"),
        new("write:key-value", "Allows creating, updating, or deleting Key Value bucket entries", "Key Value"),
        new("read:object-store", "Allows reading Object Store bucket metadata and objects", "Object Store"),
        new("write:object-store", "Allows creating, updating, or deleting Object Store buckets and objects", "Object Store"),
        new("read:services", "Allows reading NATS service discovery information", "Services"),
        new("read:audit-events", "Allows reading audit event history", "Audit"),
        new("manage:access-control", "Allows managing users, roles, and role assignments", "Access Control")
    ];
}

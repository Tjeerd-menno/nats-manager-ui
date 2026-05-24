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

    private const string JetStreamCategory = "JetStream";

    private static readonly IReadOnlyList<PermissionManifestRegistryEntry> DefaultEntries =
    [
        new("environments:read", "Allows reading registered NATS environments", "Environments"),
        new("environments:write", "Allows creating or modifying registered NATS environments", "Environments"),
        new("jetstream-streams:read", "Allows reading JetStream stream definitions and status", JetStreamCategory),
        new("jetstream-streams:write", "Allows creating, updating, or deleting JetStream streams", JetStreamCategory),
        new("jetstream-consumers:read", "Allows reading JetStream consumer definitions and status", JetStreamCategory),
        new("jetstream-consumers:write", "Allows creating, updating, or deleting JetStream consumers", JetStreamCategory),
        new("key-value:read", "Allows reading Key Value bucket metadata and entries", "Key Value"),
        new("key-value:write", "Allows creating, updating, or deleting Key Value bucket entries", "Key Value"),
        new("object-store:read", "Allows reading Object Store bucket metadata and objects", "Object Store"),
        new("object-store:write", "Allows creating, updating, or deleting Object Store buckets and objects", "Object Store"),
        new("services:read", "Allows reading NATS service discovery information", "Services"),
        new("audit-events:read", "Allows reading audit event history", "Audit"),
        new("access-control:manage", "Allows managing users, roles, and role assignments", "Access Control")
    ];
}

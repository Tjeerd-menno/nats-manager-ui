using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Services;
using Shouldly;

namespace NatsManager.Application.Tests.Modules.Permissions;

public sealed class PermissionManifestValidatorTests
{
    private readonly PermissionManifestValidator validator = new();

    [Fact]
    public void Validate_WithValidManifest_ShouldPass()
    {
        var result = validator.Validate(ValidManifest());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WithMissingApplicationId_ShouldFail()
    {
        var result = validator.Validate(ValidManifest(applicationId: " "));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Application id is required.");
    }

    [Fact]
    public void Validate_WithMissingApplicationName_ShouldFail()
    {
        var result = validator.Validate(ValidManifest(applicationName: ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Application name is required.");
    }

    [Fact]
    public void Validate_WithMissingPermissionName_ShouldFail()
    {
        var manifest = ValidManifest(permissions:
        [
            new PermissionDefinition("", "Allows reading environments", "Environments")
        ]);

        var result = validator.Validate(manifest);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Permission name is required.");
    }

    [Fact]
    public void Validate_WithMissingPermissionDescription_ShouldFail()
    {
        var manifest = ValidManifest(permissions:
        [
            new PermissionDefinition("read:environments", " ", "Environments")
        ]);

        var result = validator.Validate(manifest);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Permission description is required for 'read:environments'.");
    }

    [Fact]
    public void Validate_WithOptionalVersionAndCategory_ShouldPass()
    {
        var manifest = ValidManifest(
            applicationVersion: "1.2.3",
            permissions:
            [
                new PermissionDefinition("read:environments", "Allows reading environments", "Environments")
            ]);

        var result = validator.Validate(manifest);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCategory_ShouldFail()
    {
        var manifest = ValidManifest(permissions:
        [
            new PermissionDefinition("read:environments", "Allows reading environments", "")
        ]);

        var result = validator.Validate(manifest);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Permission category must be non-empty when provided for 'read:environments'.");
    }

    [Fact]
    public void Validate_WithEmptyApplicationVersion_ShouldFail()
    {
        var result = validator.Validate(ValidManifest(applicationVersion: " "));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Application version must be non-empty when provided.");
    }

    [Fact]
    public void Validate_WithDuplicatePermissionNameAfterTrimming_ShouldFail()
    {
        var manifest = ValidManifest(permissions:
        [
            new PermissionDefinition("read:environments", "Allows reading environments", "Environments"),
            new PermissionDefinition(" read:environments ", "Allows reading environments again", "Environments")
        ]);

        var result = validator.Validate(manifest);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("Duplicate permission name 'read:environments'.");
    }

    [Fact]
    public void PermissionManifestRegistry_WithInactiveEntry_ShouldExcludeIt()
    {
        var registry = new PermissionManifestRegistry(
        [
            new PermissionManifestRegistryEntry("read:environments", "Allows reading environments", "Environments"),
            new PermissionManifestRegistryEntry("old:environments", "Old permission", "Environments", IsActive: false)
        ]);

        var manifest = registry.CreateManifest(new ApplicationMetadata("nats-manager", "NATS Manager"));

        manifest.Permissions.Select(permission => permission.Name).ShouldBe(["read:environments"]);
    }

    private static PermissionManifest ValidManifest(
        string applicationId = "nats-manager",
        string applicationName = "NATS Manager",
        string? applicationVersion = null,
        IReadOnlyList<PermissionDefinition>? permissions = null) =>
        new(
            new ApplicationMetadata(applicationId, applicationName, applicationVersion),
            permissions ??
            [
                new PermissionDefinition("read:environments", "Allows reading environments", "Environments")
            ]);
}

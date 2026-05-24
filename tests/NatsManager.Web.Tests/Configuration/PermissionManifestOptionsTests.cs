using NatsManager.Web.Configuration;
using Shouldly;

namespace NatsManager.Web.Tests.Configuration;

public sealed class PermissionManifestOptionsTests
{
    [Fact]
    public void IsValid_WithPublicModeAndApplicationIdentity_ShouldPass()
    {
        var options = new PermissionManifestOptions
        {
            ExposureMode = PermissionManifestExposureMode.Public,
            ApplicationId = "nats-manager",
            ApplicationName = "NATS Manager"
        };

        PermissionManifestOptions.IsValid(options).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_WithMissingExposureMode_ShouldFail()
    {
        var options = new PermissionManifestOptions
        {
            ApplicationId = "nats-manager",
            ApplicationName = "NATS Manager"
        };

        PermissionManifestOptions.IsValid(options).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithRestrictedModeAndNoKeyHash_ShouldFail()
    {
        var options = new PermissionManifestOptions
        {
            ExposureMode = PermissionManifestExposureMode.Restricted,
            ApplicationId = "nats-manager",
            ApplicationName = "NATS Manager"
        };

        PermissionManifestOptions.IsValid(options).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithRestrictedModeAndKeyHash_ShouldPass()
    {
        var options = new PermissionManifestOptions
        {
            ExposureMode = PermissionManifestExposureMode.Restricted,
            RestrictedAccessKeyHash = PermissionManifestOptions.HashAccessKey("service-secret"),
            ApplicationId = "nats-manager",
            ApplicationName = "NATS Manager"
        };

        PermissionManifestOptions.IsValid(options).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_WithMissingApplicationIdentity_ShouldFail()
    {
        var options = new PermissionManifestOptions
        {
            ExposureMode = PermissionManifestExposureMode.Public,
            ApplicationId = "",
            ApplicationName = " "
        };

        PermissionManifestOptions.IsValid(options).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyVersion_ShouldFail()
    {
        var options = new PermissionManifestOptions
        {
            ExposureMode = PermissionManifestExposureMode.Public,
            ApplicationId = "nats-manager",
            ApplicationName = "NATS Manager",
            ApplicationVersion = " "
        };

        PermissionManifestOptions.IsValid(options).ShouldBeFalse();
    }
}

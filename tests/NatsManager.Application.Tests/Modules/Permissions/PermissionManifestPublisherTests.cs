using Microsoft.Extensions.Logging.Abstractions;
using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Services;
using Shouldly;

namespace NatsManager.Application.Tests.Modules.Permissions;

public sealed class PermissionManifestPublisherTests
{
    private readonly PermissionManifestPublisher publisher =
        new(new PermissionManifestValidator(), NullLogger<PermissionManifestPublisher>.Instance);

    [Fact]
    public void Publish_WithValidManifest_ShouldStoreCurrentManifest()
    {
        var manifest = Manifest("environments:read");

        var publishResult = publisher.Publish(manifest);
        var retrieval = publisher.GetManifest();

        publishResult.Published.ShouldBeTrue();
        publishResult.Status.ShouldBe(PermissionManifestPublicationStatus.Valid);
        retrieval.IsSuccess.ShouldBeTrue();
        retrieval.Manifest.ShouldBe(manifest);
        retrieval.Source.ShouldBe(PermissionManifestSource.Current);
    }

    [Fact]
    public void Publish_WithUpdatedValidManifest_ShouldReturnLatestOnNextRetrieval()
    {
        publisher.Publish(Manifest("environments:read"));
        var updated = Manifest("environments:write");

        publisher.Publish(updated);
        var retrieval = publisher.GetManifest();

        retrieval.Manifest.ShouldBe(updated);
        retrieval.Manifest!.Permissions.Select(permission => permission.Name).ShouldBe(["environments:write"]);
        retrieval.Source.ShouldBe(PermissionManifestSource.Current);
    }

    [Fact]
    public void Publish_WithInvalidManifestAfterValidPublish_ShouldReturnLastValidManifest()
    {
        var valid = Manifest("environments:read");
        publisher.Publish(valid);

        var publishResult = publisher.Publish(Manifest(
            "environments:read",
            new PermissionDefinition(" environments:read ", "Duplicate permission", "Environments")));
        var retrieval = publisher.GetManifest();

        publishResult.Published.ShouldBeFalse();
        publishResult.Status.ShouldBe(PermissionManifestPublicationStatus.InvalidWithFallback);
        retrieval.IsSuccess.ShouldBeTrue();
        retrieval.Manifest.ShouldBe(valid);
        retrieval.Source.ShouldBe(PermissionManifestSource.LastValid);
        retrieval.FailureReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Publish_WithInvalidManifestAndNoPriorValidManifest_ShouldFailSafely()
    {
        var publishResult = publisher.Publish(Manifest(""));
        var retrieval = publisher.GetManifest();

        publishResult.Published.ShouldBeFalse();
        publishResult.Status.ShouldBe(PermissionManifestPublicationStatus.InvalidNoFallback);
        retrieval.IsSuccess.ShouldBeFalse();
        retrieval.Manifest.ShouldBeNull();
        retrieval.FailureReason.ShouldNotBeNullOrWhiteSpace();
    }

    private static PermissionManifest Manifest(params object[] permissions) =>
        new(
            new ApplicationMetadata("nats-manager", "NATS Manager"),
            permissions.Select(permission => permission switch
            {
                string name => new PermissionDefinition(name, "Allows access", "General"),
                PermissionDefinition definition => definition,
                _ => throw new ArgumentException("Unsupported permission test value.", nameof(permissions))
            }).ToArray());
}

using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Application.Modules.Relationships.Models;
using NatsManager.Application.Modules.Relationships.Ports;
using NatsManager.Application.Modules.Services.Ports;

namespace NatsManager.Infrastructure.Relationships;

/// <summary>
/// Resolves focal resources by dispatching to the appropriate module adapter
/// based on the resource type. Returns null when the resource does not exist.
/// </summary>
public sealed class FocalResourceResolver(
    IJetStreamAdapter jetStreamAdapter,
    IKvStoreAdapter kvStoreAdapter,
    IObjectStoreAdapter objectStoreAdapter,
    IServiceDiscoveryAdapter serviceDiscoveryAdapter,
    ICoreNatsAdapter coreNatsAdapter) : IFocalResourceResolver
{
    public async Task<FocalResource?> ResolveAsync(
        Guid environmentId,
        ResourceType resourceType,
        string resourceId,
        CancellationToken ct)
    {
        return resourceType switch
        {
            ResourceType.Stream => await ResolveStreamAsync(environmentId, resourceId, ct),
            ResourceType.Consumer => await ResolveConsumerAsync(environmentId, resourceId, ct),
            ResourceType.KvBucket => await ResolveKvBucketAsync(environmentId, resourceId, ct),
            ResourceType.KvKey => await ResolveKvKeyAsync(environmentId, resourceId, ct),
            ResourceType.ObjectBucket => await ResolveObjectBucketAsync(environmentId, resourceId, ct),
            ResourceType.ObjectStoreObject => await ResolveObjectAsync(environmentId, resourceId, ct),
            ResourceType.Service => await ResolveServiceAsync(environmentId, resourceId, ct),
            ResourceType.Subject => await ResolveSubjectAsync(environmentId, resourceId, ct),
            ResourceType.Server => await ResolveServerAsync(environmentId, resourceId, ct),
            _ => null
        };
    }

    private async Task<FocalResource?> ResolveStreamAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var stream = await jetStreamAdapter.GetStreamAsync(environmentId, resourceId, ct);
        if (stream == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.Stream,
            ResourceId: resourceId,
            DisplayName: stream.Name,
            Route: $"/jetstream/streams/{Uri.EscapeDataString(resourceId)}");
    }

    private async Task<FocalResource?> ResolveConsumerAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var parts = resourceId.Split('/', 2);
        if (parts.Length != 2) return null;
        var consumer = await jetStreamAdapter.GetConsumerAsync(environmentId, parts[0], parts[1], ct);
        if (consumer == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.Consumer,
            ResourceId: resourceId,
            DisplayName: consumer.Name,
            Route: $"/jetstream/streams/{Uri.EscapeDataString(parts[0])}/consumers/{Uri.EscapeDataString(parts[1])}");
    }

    private async Task<FocalResource?> ResolveKvBucketAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var bucket = await kvStoreAdapter.GetBucketAsync(environmentId, resourceId, ct);
        if (bucket == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.KvBucket,
            ResourceId: resourceId,
            DisplayName: bucket.BucketName,
            Route: $"/kv/buckets/{Uri.EscapeDataString(resourceId)}");
    }

    private async Task<FocalResource?> ResolveKvKeyAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var parts = resourceId.Split('/', 2);
        if (parts.Length != 2) return null;
        var key = await kvStoreAdapter.GetKeyAsync(environmentId, parts[0], parts[1], ct);
        if (key == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.KvKey,
            ResourceId: resourceId,
            DisplayName: key.Key,
            Route: $"/kv/buckets/{Uri.EscapeDataString(parts[0])}/keys/{Uri.EscapeDataString(parts[1])}");
    }

    private async Task<FocalResource?> ResolveObjectBucketAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var bucket = await objectStoreAdapter.GetBucketAsync(environmentId, resourceId, ct);
        if (bucket == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.ObjectBucket,
            ResourceId: resourceId,
            DisplayName: bucket.BucketName,
            Route: $"/objectstore/buckets/{Uri.EscapeDataString(resourceId)}");
    }

    private async Task<FocalResource?> ResolveObjectAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var parts = resourceId.Split('/', 2);
        if (parts.Length != 2) return null;
        var obj = await objectStoreAdapter.GetObjectInfoAsync(environmentId, parts[0], parts[1], ct);
        if (obj == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.ObjectStoreObject,
            ResourceId: resourceId,
            DisplayName: obj.Name,
            Route: $"/objectstore/buckets/{Uri.EscapeDataString(parts[0])}/objects/{Uri.EscapeDataString(parts[1])}");
    }

    private async Task<FocalResource?> ResolveServiceAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var service = await serviceDiscoveryAdapter.GetServiceAsync(environmentId, resourceId, ct);
        if (service == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.Service,
            ResourceId: resourceId,
            DisplayName: service.Name,
            Route: $"/services/{Uri.EscapeDataString(resourceId)}");
    }

    private async Task<FocalResource?> ResolveSubjectAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        _ = await coreNatsAdapter.ListSubjectsAsync(environmentId, ct);
        // subjects may not always appear in the listing; return a synthetic focal resource if not found
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.Subject,
            ResourceId: resourceId,
            DisplayName: resourceId,
            Route: null);
    }

    private async Task<FocalResource?> ResolveServerAsync(Guid environmentId, string resourceId, CancellationToken ct)
    {
        var serverInfo = await coreNatsAdapter.GetServerInfoAsync(environmentId, ct);
        if (serverInfo == null) return null;
        return new FocalResource(
            EnvironmentId: environmentId,
            ResourceType: ResourceType.Server,
            ResourceId: resourceId,
            DisplayName: serverInfo.ServerName,
            Route: null);
    }
}

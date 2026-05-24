using NatsManager.Application.Common;
using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Services;

namespace NatsManager.Application.Modules.Permissions.Queries;

public sealed record GetPermissionManifestQuery;

public sealed class GetPermissionManifestQueryHandler(
    IPermissionManifestPublisher publisher)
    : IUseCase<GetPermissionManifestQuery, ManifestRetrievalResult>
{
    private readonly IPermissionManifestPublisher publisher = publisher;

    public Task ExecuteAsync(
        GetPermissionManifestQuery request,
        IOutputPort<ManifestRetrievalResult> outputPort,
        CancellationToken cancellationToken = default)
    {
        outputPort.Success(publisher.GetManifest());
        return Task.CompletedTask;
    }
}

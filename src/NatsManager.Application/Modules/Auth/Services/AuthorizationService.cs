namespace NatsManager.Application.Modules.Auth.Services;

public interface IAuthorizationService
{
    Task<bool> CanPerformActionAsync(
        Guid userId,
        string requiredRole,
        Guid? environmentId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsProductionRestricted(
        Guid userId,
        Guid environmentId,
        CancellationToken cancellationToken = default);
}

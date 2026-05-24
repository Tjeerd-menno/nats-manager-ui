using Microsoft.Extensions.Logging;
using NatsManager.Application.Modules.Permissions.Models;

namespace NatsManager.Application.Modules.Permissions.Services;

public interface IPermissionManifestPublisher
{
    PermissionManifestPublicationState State { get; }

    PermissionManifestPublicationResult Publish(PermissionManifest? manifest);

    ManifestRetrievalResult GetManifest();
}

public sealed record PermissionManifestPublicationResult(
    PermissionManifestPublicationStatus Status,
    bool Published,
    string? FailureReason);

public sealed class PermissionManifestPublisher(
    IPermissionManifestValidator validator,
    ILogger<PermissionManifestPublisher> logger) : IPermissionManifestPublisher
{
    private static readonly Action<ILogger, PermissionManifestPublicationStatus, string, Exception?> LogManifestValidationFailed =
        LoggerMessage.Define<PermissionManifestPublicationStatus, string>(
            LogLevel.Warning,
            new EventId(1000, nameof(LogManifestValidationFailed)),
            "Permission manifest validation failed with status {Status}: {FailureReason}");

    private readonly IPermissionManifestValidator validator = validator;
    private readonly ILogger<PermissionManifestPublisher> logger = logger;
    private readonly object gate = new();
    private PermissionManifestPublicationState state =
        new(null, null, null, PermissionManifestPublicationStatus.InvalidNoFallback);

    public PermissionManifestPublicationState State
    {
        get
        {
            lock (gate)
            {
                return state;
            }
        }
    }

    public PermissionManifestPublicationResult Publish(PermissionManifest? manifest)
    {
        var validation = validator.Validate(manifest);
        var validatedAt = DateTimeOffset.UtcNow;

        lock (gate)
        {
            if (validation.IsValid)
            {
                state = new PermissionManifestPublicationState(
                    manifest,
                    validatedAt,
                    null,
                    PermissionManifestPublicationStatus.Valid);

                return new PermissionManifestPublicationResult(
                    PermissionManifestPublicationStatus.Valid,
                    Published: true,
                    FailureReason: null);
            }

            var failureReason = string.Join("; ", validation.Errors);
            var status = state.LastValidManifest is null
                ? PermissionManifestPublicationStatus.InvalidNoFallback
                : PermissionManifestPublicationStatus.InvalidWithFallback;

            LogManifestValidationFailed(logger, status, failureReason, null);

            state = new PermissionManifestPublicationState(
                state.LastValidManifest,
                validatedAt,
                failureReason,
                status);

            return new PermissionManifestPublicationResult(
                status,
                Published: false,
                failureReason);
        }
    }

    public ManifestRetrievalResult GetManifest()
    {
        lock (gate)
        {
            if (state.Status == PermissionManifestPublicationStatus.Valid
                && state.LastValidManifest is not null)
            {
                return ManifestRetrievalResult.Current(state.LastValidManifest);
            }

            if (state.Status == PermissionManifestPublicationStatus.InvalidWithFallback
                && state.LastValidManifest is not null)
            {
                return ManifestRetrievalResult.LastValid(
                    state.LastValidManifest,
                    state.LastFailureReason);
            }

            return ManifestRetrievalResult.Failure(
                state.LastFailureReason ?? "No valid permission manifest is currently available.");
        }
    }
}

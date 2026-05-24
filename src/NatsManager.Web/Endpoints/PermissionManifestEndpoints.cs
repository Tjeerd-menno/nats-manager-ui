using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Queries;
using NatsManager.Web.Configuration;
using NatsManager.Web.Presenters;

namespace NatsManager.Web.Endpoints;

public static class PermissionManifestEndpoints
{
    private const string AccessKeyHeaderName = "X-Permission-Manifest-Key";
    private const string SourceHeaderName = "X-Permission-Manifest-Source";
    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Action<ILogger, PermissionManifestExposureMode, Exception?> LogRetrievalDenied =
        LoggerMessage.Define<PermissionManifestExposureMode>(
            LogLevel.Warning,
            new EventId(1000, nameof(LogRetrievalDenied)),
            "Permission manifest retrieval denied for exposure mode {ExposureMode}");

    private static readonly Action<ILogger, string?, Exception?> LogRetrievalFailed =
        LoggerMessage.Define<string?>(
            LogLevel.Error,
            new EventId(1001, nameof(LogRetrievalFailed)),
            "Permission manifest retrieval failed with reason {FailureReason}");

    private static readonly Action<ILogger, string, int, Exception?> LogRetrievalSucceeded =
        LoggerMessage.Define<string, int>(
            LogLevel.Information,
            new EventId(1002, nameof(LogRetrievalSucceeded)),
            "Permission manifest retrieved with source {ManifestSource} and permission count {PermissionCount}");

    private static readonly Action<ILogger, string?, Exception?> LogLastValidServed =
        LoggerMessage.Define<string?>(
            LogLevel.Warning,
            new EventId(1003, nameof(LogLastValidServed)),
            "Permission manifest retrieval served last valid manifest because current manifest failed validation: {FailureReason}");

    public static IEndpointRouteBuilder MapPermissionManifestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/.well-known/permissions", GetPermissionManifest)
            .WithTags("Permissions")
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> GetPermissionManifest(
        HttpContext httpContext,
        IOptions<PermissionManifestOptions> optionsAccessor,
        IUseCase<GetPermissionManifestQuery, ManifestRetrievalResult> useCase,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("NatsManager.Web.Endpoints.PermissionManifestEndpoints");
        var options = optionsAccessor.Value;

        if (!IsAuthorized(httpContext.Request, options))
        {
            LogRetrievalDenied(logger, options.ExposureMode, null);

            return ApiProblemResults.Unauthorized("A valid permission manifest access key is required.");
        }

        var presenter = new Presenter<ManifestRetrievalResult>();
        await useCase.ExecuteAsync(new GetPermissionManifestQuery(), presenter, cancellationToken);

        if (!presenter.IsSuccess || presenter.Value is null)
        {
            return presenter.ToResult();
        }

        var retrieval = presenter.Value;
        if (retrieval.Manifest is null)
        {
            LogRetrievalFailed(logger, retrieval.FailureReason, null);

            return ApiProblemResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Permission manifest unavailable",
                detail: "No valid permission manifest is currently available.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.6.4");
        }

        var source = retrieval.Source == PermissionManifestSource.LastValid
            ? "last-valid"
            : "current";
        httpContext.Response.Headers[SourceHeaderName] = source;

        LogRetrievalSucceeded(logger, source, retrieval.Manifest.Permissions.Count, null);

        if (retrieval.Source == PermissionManifestSource.LastValid)
        {
            LogLastValidServed(logger, retrieval.FailureReason, null);
        }

        return Results.Json(retrieval.Manifest, ManifestJsonOptions);
    }

    private static bool IsAuthorized(HttpRequest request, PermissionManifestOptions options) =>
        options.ExposureMode switch
        {
            PermissionManifestExposureMode.Public => true,
            PermissionManifestExposureMode.Restricted => HasValidAccessKey(request, options.RestrictedAccessKeyHash),
            _ => false
        };

    private static bool HasValidAccessKey(HttpRequest request, string? configuredHash)
    {
        if (string.IsNullOrWhiteSpace(configuredHash)
            || !request.Headers.TryGetValue(AccessKeyHeaderName, out var values))
        {
            return false;
        }

        var accessKey = values.FirstOrDefault();
        if (string.IsNullOrEmpty(accessKey))
        {
            return false;
        }

        byte[] expectedHash;
        try
        {
            expectedHash = Convert.FromBase64String(configuredHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expectedHash.Length != 32)
        {
            return false;
        }

        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(accessKey));
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

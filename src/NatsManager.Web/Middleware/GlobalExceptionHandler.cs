using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NatsManager.Domain.Modules.Common.Errors;

namespace NatsManager.Web.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation Error",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                Extensions =
                {
                    ["errors"] = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFoundEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Extensions =
                {
                    ["errorCode"] = notFoundEx.ErrorCode,
                    ["resourceType"] = notFoundEx.ResourceType,
                    ["resourceId"] = notFoundEx.ResourceId
                }
            },
            ConflictException conflictEx => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflictEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Extensions = { ["errorCode"] = conflictEx.ErrorCode }
            },
            UnauthorizedAccessException unauthorizedEx => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = unauthorizedEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2"
            },
            ForbiddenException forbiddenEx => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = forbiddenEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                Extensions = { ["errorCode"] = forbiddenEx.ErrorCode }
            },
            ConnectionException connectionEx => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Connection Error",
                Detail = connectionEx.Message,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.3",
                Extensions =
                {
                    ["errorCode"] = connectionEx.ErrorCode,
                    ["environmentName"] = connectionEx.EnvironmentName
                }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            }
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

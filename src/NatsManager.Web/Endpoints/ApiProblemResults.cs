using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace NatsManager.Web.Endpoints;

internal static class ApiProblemResults
{
    public static IResult ValidationProblem(string field, string message)
        => Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = [message]
        });

    public static IResult ValidationProblem(IEnumerable<ValidationFailure> failures)
        => Results.ValidationProblem(failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray()));

    public static IResult ConfirmationRequired(string message)
        => ValidationProblem("X-Confirm", message);

    public static IResult BadRequest(string detail)
        => Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.1");

    public static IResult NotFound(string detail)
        => Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");

    public static IResult Conflict(string detail)
        => Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.10");

    public static IResult Unauthorized(string detail)
        => Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.2");

    public static IResult Forbidden(string detail)
        => Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden",
            detail: detail,
            type: "https://tools.ietf.org/html/rfc9110#section-15.5.4");

    public static IResult Problem(
        int statusCode,
        string title,
        string? detail = null,
        string? type = null,
        IDictionary<string, object?>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type
        };

        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions[extension.Key] = extension.Value;
            }
        }

        return Results.Json(
            problemDetails,
            statusCode: statusCode,
            contentType: "application/problem+json");
    }
}

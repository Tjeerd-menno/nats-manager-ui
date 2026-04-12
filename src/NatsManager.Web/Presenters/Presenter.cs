using NatsManager.Application.Common;

namespace NatsManager.Web.Presenters;

public sealed class Presenter<T> : IOutputPort<T>
{
    public T? Value { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsNotFound { get; private set; }
    public bool IsConflict { get; private set; }
    public bool IsUnauthorized { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ResourceType { get; private set; }
    public string? ResourceId { get; private set; }

    public void Success(T result)
    {
        Value = result;
        IsSuccess = true;
    }

    public void NotFound(string resourceType, string resourceId)
    {
        IsNotFound = true;
        ResourceType = resourceType;
        ResourceId = resourceId;
        ErrorMessage = $"{resourceType} '{resourceId}' not found.";
    }

    public void Conflict(string message)
    {
        IsConflict = true;
        ErrorMessage = message;
    }

    public void Unauthorized(string message)
    {
        IsUnauthorized = true;
        ErrorMessage = message;
    }

    public IResult ToResult()
    {
        if (IsSuccess)
            return typeof(T) == typeof(Unit) ? Results.Ok() : Results.Ok(Value);

        if (IsNotFound)
            return Results.NotFound(new { error = ErrorMessage, resourceType = ResourceType, resourceId = ResourceId });

        if (IsConflict)
            return Results.Conflict(new { error = ErrorMessage });

        if (IsUnauthorized)
            return Results.Json(new { error = ErrorMessage }, statusCode: StatusCodes.Status401Unauthorized);

        return Results.Problem("An unexpected error occurred.");
    }

    public IResult ToCreatedResult(string uri)
    {
        if (IsSuccess)
            return Results.Created(uri, Value);

        return ToResult();
    }

    public IResult ToNoContentResult()
    {
        if (IsSuccess)
            return Results.NoContent();

        return ToResult();
    }
}

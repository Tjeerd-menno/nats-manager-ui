using NatsManager.Application.Common;

namespace NatsManager.Application.Tests;

public sealed class TestOutputPort<T> : IOutputPort<T>
{
    public T? Value { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool IsNotFound { get; private set; }
    public bool IsConflict { get; private set; }
    public bool IsUnauthorized { get; private set; }
    public bool IsForbidden { get; private set; }
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

    public void Forbidden(string message)
    {
        IsForbidden = true;
        ErrorMessage = message;
    }
}

namespace NatsManager.Application.Common;

public interface IOutputPort<in TResult>
{
    void Success(TResult result);
    void NotFound(string resourceType, string resourceId);
    void Conflict(string message);
    void Unauthorized(string message);

    /// <summary>
    /// The request was authenticated but the caller is not permitted to perform the action.
    /// Maps to HTTP 403.
    /// </summary>
    void Forbidden(string message);
}

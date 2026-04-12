namespace NatsManager.Application.Common;

public interface IOutputPort<in TResult>
{
    void Success(TResult result);
    void NotFound(string resourceType, string resourceId);
    void Conflict(string message);
    void Unauthorized(string message);
}

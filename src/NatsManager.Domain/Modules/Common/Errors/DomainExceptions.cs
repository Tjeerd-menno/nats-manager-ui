namespace NatsManager.Domain.Modules.Common.Errors;

public abstract class DomainException(string message) : Exception(message)
{
    public abstract string ErrorCode { get; }
}

public sealed class NotFoundException(string resourceType, string resourceId)
    : DomainException($"{resourceType} '{resourceId}' was not found")
{
    public override string ErrorCode => "RESOURCE_NOT_FOUND";
    public string ResourceType { get; } = resourceType;
    public string ResourceId { get; } = resourceId;
}

public sealed class ConflictException(string message) : DomainException(message)
{
    public override string ErrorCode => "CONFLICT";
}

public sealed class ForbiddenException(string message) : DomainException(message)
{
    public override string ErrorCode => "FORBIDDEN";
}

public sealed class ConnectionException(string environmentName, string message)
    : DomainException($"Connection to '{environmentName}' failed: {message}")
{
    public override string ErrorCode => "CONNECTION_ERROR";
    public string EnvironmentName { get; } = environmentName;
}

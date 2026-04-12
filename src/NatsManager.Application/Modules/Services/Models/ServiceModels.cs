namespace NatsManager.Application.Modules.Services.Models;

public sealed record ServiceInfo(
    string Name,
    string Id,
    string Version,
    string Description,
    IReadOnlyList<ServiceEndpoint> Endpoints,
    ServiceStats? Stats);

public sealed record ServiceEndpoint(
    string Name,
    string Subject,
    string? QueueGroup);

public sealed record ServiceStats(
    int NumRequests,
    int NumErrors,
    TimeSpan ProcessingTime,
    DateTimeOffset Started);

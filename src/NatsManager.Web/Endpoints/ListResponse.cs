namespace NatsManager.Web.Endpoints;

public sealed record ListResponse<T>(IReadOnlyList<T> Items);

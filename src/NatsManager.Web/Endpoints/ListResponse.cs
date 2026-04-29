namespace NatsManager.Web.Endpoints;

public sealed record ListResponse<T>(IReadOnlyList<T> Items)
{
    public static ListResponse<T> From(IReadOnlyList<T> items) => new(items);
}

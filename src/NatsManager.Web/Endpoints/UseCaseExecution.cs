using NatsManager.Application.Common;
using NatsManager.Web.Presenters;

namespace NatsManager.Web.Endpoints;

/// <summary>
/// Helper extensions that collapse the common "create presenter, execute, project result"
/// boilerplate in Minimal API endpoint handlers into a single call. Endpoints that need to
/// inspect the presenter directly (for example to shape a custom success payload) should keep
/// using <see cref="Presenter{T}"/> explicitly.
/// </summary>
public static class UseCaseExecution
{
    public static async Task<IResult> ExecuteToResultAsync<TRequest, TResult>(
        this IUseCase<TRequest, TResult> useCase,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var presenter = new Presenter<TResult>();
        await useCase.ExecuteAsync(request, presenter, cancellationToken);
        return presenter.ToResult();
    }

    public static async Task<IResult> ExecuteToCreatedResultAsync<TRequest, TResult>(
        this IUseCase<TRequest, TResult> useCase,
        TRequest request,
        string uri,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var presenter = new Presenter<TResult>();
        await useCase.ExecuteAsync(request, presenter, cancellationToken);
        return presenter.ToCreatedResult(uri);
    }

    public static async Task<IResult> ExecuteToCreatedResultAsync<TRequest, TResult>(
        this IUseCase<TRequest, TResult> useCase,
        TRequest request,
        Func<TResult, string> uriFactory,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var presenter = new Presenter<TResult>();
        await useCase.ExecuteAsync(request, presenter, cancellationToken);
        return presenter.ToCreatedResult(uriFactory);
    }

    public static async Task<IResult> ExecuteToNoContentResultAsync<TRequest, TResult>(
        this IUseCase<TRequest, TResult> useCase,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : notnull
    {
        var presenter = new Presenter<TResult>();
        await useCase.ExecuteAsync(request, presenter, cancellationToken);
        return presenter.ToNoContentResult();
    }
}


namespace NatsManager.Application.Common;

public interface IUseCase<in TRequest, TResult> where TRequest : notnull
{
    Task ExecuteAsync(TRequest request, IOutputPort<TResult> outputPort, CancellationToken cancellationToken = default);
}

public readonly record struct Unit
{
    /// <summary>The single <see cref="Unit"/> value.</summary>
    public static Unit Value => default;
}

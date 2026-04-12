namespace NatsManager.Application.Common;

public interface IUseCase<in TRequest, TResult> where TRequest : notnull
{
    Task ExecuteAsync(TRequest request, IOutputPort<TResult> outputPort, CancellationToken cancellationToken = default);
}

public readonly record struct Unit
{
#pragma warning disable CA1805 // Do not initialize unnecessarily
    public static readonly Unit Value = new();
#pragma warning restore CA1805
}

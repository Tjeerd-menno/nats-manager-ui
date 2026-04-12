using FluentValidation;
using NatsManager.Application.Common;

namespace NatsManager.Application.Behaviors;

public sealed class ValidatedUseCase<TRequest, TResponse>(
    IUseCase<TRequest, TResponse> inner,
    IEnumerable<IValidator<TRequest>> validators) : IUseCase<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task ExecuteAsync(TRequest request, IOutputPort<TResponse> outputPort, CancellationToken cancellationToken = default)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        await inner.ExecuteAsync(request, outputPort, cancellationToken);
    }
}

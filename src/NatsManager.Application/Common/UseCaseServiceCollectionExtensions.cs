using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using NatsManager.Application.Behaviors;

namespace NatsManager.Application.Common;

public static class UseCaseServiceCollectionExtensions
{
    public static IServiceCollection AddUseCases(this IServiceCollection services, Assembly assembly)
    {
        var useCaseType = typeof(IUseCase<,>);

        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == useCaseType)
                .Select(i => new { InterfaceType = i, ImplementationType = t }))
            .ToList();

        foreach (var impl in implementations)
        {
            services.AddScoped(impl.ImplementationType);

            var requestType = impl.InterfaceType.GenericTypeArguments[0];
            var responseType = impl.InterfaceType.GenericTypeArguments[1];

            services.AddScoped(impl.InterfaceType, sp =>
            {
                var inner = sp.GetRequiredService(impl.ImplementationType);

                // Wrap with validation
                var validatorEnumerableType = typeof(IEnumerable<>).MakeGenericType(
                    typeof(IValidator<>).MakeGenericType(requestType));
                var validators = sp.GetRequiredService(validatorEnumerableType);
                var validatedType = typeof(ValidatedUseCase<,>).MakeGenericType(requestType, responseType);
                inner = Activator.CreateInstance(validatedType, inner, validators)!;

                return inner;
            });
        }

        // Register audit trail service
        services.AddScoped<IAuditTrail, AuditTrail>();

        return services;
    }
}

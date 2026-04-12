using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common.Errors;
using NatsManager.Infrastructure.Persistence;

namespace NatsManager.Infrastructure.Nats;

public sealed class EnvironmentConnectionResolver(
    IServiceScopeFactory scopeFactory,
    ICredentialEncryptionService encryptionService) : IEnvironmentConnectionResolver
{
    public async Task<EnvironmentConnectionInfo> ResolveAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var environment = await context.Environments
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == environmentId, cancellationToken)
            ?? throw new NotFoundException("Environment", environmentId.ToString());

        string? credential = null;
        if (environment.CredentialType != Domain.Modules.Common.CredentialType.None
            && !string.IsNullOrEmpty(environment.CredentialReference))
        {
            credential = encryptionService.Decrypt(environment.CredentialReference);
        }

        return new EnvironmentConnectionInfo(
            environment.ServerUrl,
            environment.Name,
            environment.IsEnabled,
            environment.CredentialType,
            credential);
    }
}

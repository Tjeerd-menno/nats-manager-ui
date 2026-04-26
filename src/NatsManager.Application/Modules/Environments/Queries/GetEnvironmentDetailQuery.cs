using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;

namespace NatsManager.Application.Modules.Environments.Queries;

public sealed record GetEnvironmentDetailQuery(Guid Id);

public sealed record EnvironmentDetailResult(
    Guid Id,
    string Name,
    string Description,
    string ServerUrl,
    string CredentialType,
    bool IsEnabled,
    bool IsProduction,
    string ConnectionStatus,
    DateTimeOffset? LastSuccessfulContact,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? MonitoringUrl,
    int? MonitoringPollingIntervalSeconds);

public sealed class GetEnvironmentDetailQueryHandler(
    IEnvironmentRepository environmentRepository) : IUseCase<GetEnvironmentDetailQuery, EnvironmentDetailResult>
{
    public async Task ExecuteAsync(GetEnvironmentDetailQuery request, IOutputPort<EnvironmentDetailResult> outputPort, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (environment is null) { outputPort.NotFound("Environment", request.Id.ToString()); return; }

        outputPort.Success(new EnvironmentDetailResult(
            environment.Id,
            environment.Name,
            environment.Description,
            environment.ServerUrl,
            environment.CredentialType.ToString(),
            environment.IsEnabled,
            environment.IsProduction,
            environment.ConnectionStatus.ToString(),
            environment.LastSuccessfulContact,
            environment.CreatedAt,
            environment.UpdatedAt,
            environment.MonitoringUrl,
            environment.MonitoringPollingIntervalSeconds));
    }
}

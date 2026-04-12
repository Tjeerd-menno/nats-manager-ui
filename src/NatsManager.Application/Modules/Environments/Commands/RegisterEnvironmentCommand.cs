using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Modules.Environments.Commands;

public sealed record RegisterEnvironmentCommand : IAuditableCommand
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ServerUrl { get; init; }
    public CredentialType CredentialType { get; init; } = CredentialType.None;
    public string? Credential { get; init; }
    public bool IsProduction { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Environment;
    string IAuditableCommand.ResourceId => string.Empty;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => null;
}

public sealed record RegisterEnvironmentResult(Guid Id, string Name);

public sealed class RegisterEnvironmentCommandValidator : AbstractValidator<RegisterEnvironmentCommand>
{
    public RegisterEnvironmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ServerUrl).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Credential).NotEmpty()
            .When(x => x.CredentialType != CredentialType.None)
            .WithMessage("Credential is required when CredentialType is not None.");
    }
}

public sealed class RegisterEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    ICredentialEncryptionService encryptionService,
    INatsHealthChecker healthChecker,
    IAuditTrail auditTrail) : IUseCase<RegisterEnvironmentCommand, RegisterEnvironmentResult>
{
    public async Task ExecuteAsync(RegisterEnvironmentCommand request, IOutputPort<RegisterEnvironmentResult> outputPort, CancellationToken cancellationToken)
    {
        if (await environmentRepository.ExistsWithNameAsync(request.Name, cancellationToken: cancellationToken))
        {
            outputPort.Conflict($"Environment with name '{request.Name}' already exists.");
            return;
        }

        string? credentialRef = null;
        if (request.CredentialType != CredentialType.None && !string.IsNullOrEmpty(request.Credential))
        {
            credentialRef = encryptionService.Encrypt(request.Credential);
        }

        var environment = Environment.Create(
            name: request.Name,
            serverUrl: request.ServerUrl,
            description: request.Description,
            credentialType: request.CredentialType,
            credentialReference: credentialRef,
            isProduction: request.IsProduction);

        try
        {
            var healthResult = await healthChecker.CheckHealthAsync(environment, cancellationToken);
            environment.UpdateConnectionStatus(
                healthResult.Reachable ? ConnectionStatus.Available : ConnectionStatus.Unavailable);
        }
        catch
        {
            environment.UpdateConnectionStatus(ConnectionStatus.Unavailable);
        }

        await environmentRepository.AddAsync(environment, cancellationToken);

        await auditTrail.RecordAsync(request, environment.Id.ToString(), cancellationToken);
        outputPort.Success(new RegisterEnvironmentResult(environment.Id, environment.Name));
    }
}

using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Commands;

public sealed record UpdateEnvironmentCommand : IAuditableCommand
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string ServerUrl { get; init; }
    public CredentialType CredentialType { get; init; } = CredentialType.None;
    public string? Credential { get; init; }
    public bool IsProduction { get; init; }
    public bool IsEnabled { get; init; } = true;

    ActionType IAuditableCommand.ActionType => ActionType.Update;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Environment;
    string IAuditableCommand.ResourceId => Id.ToString();
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => Id;
}

public sealed class UpdateEnvironmentCommandValidator : AbstractValidator<UpdateEnvironmentCommand>
{
    public UpdateEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ServerUrl).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class UpdateEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    ICredentialEncryptionService encryptionService,
    INatsConnectionFactory natsConnectionFactory,
    IAuditTrail auditTrail) : IUseCase<UpdateEnvironmentCommand, Unit>
{
    public async Task ExecuteAsync(UpdateEnvironmentCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (environment is null)
        {
            outputPort.NotFound("Environment", request.Id.ToString());
            return;
        }

        if (await environmentRepository.ExistsWithNameAsync(request.Name, request.Id, cancellationToken))
        {
            outputPort.Conflict($"Environment with name '{request.Name}' already exists.");
            return;
        }

        string? credentialRef = null;
        if (request.CredentialType != CredentialType.None && !string.IsNullOrEmpty(request.Credential))
        {
            credentialRef = encryptionService.Encrypt(request.Credential);
        }

        environment.Update(
            name: request.Name,
            serverUrl: request.ServerUrl,
            description: request.Description,
            credentialType: request.CredentialType,
            credentialReference: credentialRef,
            isProduction: request.IsProduction);

        if (request.IsEnabled && !environment.IsEnabled)
        {
            environment.Enable();
        }
        else if (!request.IsEnabled && environment.IsEnabled)
        {
            environment.Disable();
        }

        await environmentRepository.UpdateAsync(environment, cancellationToken);

        // Remove cached connection so it reconnects with new settings
        await natsConnectionFactory.RemoveConnectionAsync(request.Id, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

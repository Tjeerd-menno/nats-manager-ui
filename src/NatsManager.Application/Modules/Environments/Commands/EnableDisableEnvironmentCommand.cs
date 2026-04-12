using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Commands;

public sealed record EnableDisableEnvironmentCommand : IAuditableCommand
{
    public required Guid Id { get; init; }
    public required bool Enable { get; init; }
    internal string? ResolvedName { get; set; }

    ActionType IAuditableCommand.ActionType => ActionType.Update;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Environment;
    string IAuditableCommand.ResourceId => Id.ToString();
    string IAuditableCommand.ResourceName => ResolvedName ?? Id.ToString();
    Guid? IAuditableCommand.EnvironmentId => Id;
}

public sealed class EnableDisableEnvironmentCommandValidator : AbstractValidator<EnableDisableEnvironmentCommand>
{
    public EnableDisableEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class EnableDisableEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    INatsConnectionFactory natsConnectionFactory,
    IAuditTrail auditTrail) : IUseCase<EnableDisableEnvironmentCommand, Unit>
{
    public async Task ExecuteAsync(EnableDisableEnvironmentCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (environment is null)
        {
            outputPort.NotFound("Environment", request.Id.ToString());
            return;
        }

        request.ResolvedName = environment.Name;

        if (request.Enable)
        {
            environment.Enable();
        }
        else
        {
            environment.Disable();
            await natsConnectionFactory.RemoveConnectionAsync(request.Id, cancellationToken);
        }

        await environmentRepository.UpdateAsync(environment, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

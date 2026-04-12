using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.Environments.Commands;

public sealed record DeleteEnvironmentCommand : IAuditableCommand
{
    public required Guid Id { get; init; }
    internal string? ResolvedName { get; set; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Environment;
    string IAuditableCommand.ResourceId => Id.ToString();
    string IAuditableCommand.ResourceName => ResolvedName ?? Id.ToString();
    Guid? IAuditableCommand.EnvironmentId => Id;
}

public sealed class DeleteEnvironmentCommandValidator : AbstractValidator<DeleteEnvironmentCommand>
{
    public DeleteEnvironmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteEnvironmentCommandHandler(
    IEnvironmentRepository environmentRepository,
    INatsConnectionFactory natsConnectionFactory,
    IAuditTrail auditTrail) : IUseCase<DeleteEnvironmentCommand, Unit>
{
    public async Task ExecuteAsync(DeleteEnvironmentCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var environment = await environmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (environment is null)
        {
            outputPort.NotFound("Environment", request.Id.ToString());
            return;
        }

        request.ResolvedName = environment.Name;
        await natsConnectionFactory.RemoveConnectionAsync(request.Id, cancellationToken);
        await environmentRepository.DeleteAsync(environment, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

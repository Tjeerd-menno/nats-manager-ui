using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.JetStream.Commands;

public sealed record DeleteStreamCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Stream;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteStreamCommandValidator : AbstractValidator<DeleteStreamCommand>
{
    public DeleteStreamCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class DeleteStreamCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<DeleteStreamCommand, Unit>
{
    public async Task ExecuteAsync(DeleteStreamCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.DeleteStreamAsync(request.EnvironmentId, request.Name, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed record PurgeStreamCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Stream;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class PurgeStreamCommandValidator : AbstractValidator<PurgeStreamCommand>
{
    public PurgeStreamCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class PurgeStreamCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<PurgeStreamCommand, Unit>
{
    public async Task ExecuteAsync(PurgeStreamCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.PurgeStreamAsync(request.EnvironmentId, request.Name, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

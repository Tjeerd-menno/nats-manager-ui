using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.JetStream.Commands;

public sealed record CreateConsumerCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string StreamName { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string DeliverPolicy { get; init; } = "All";
    public string AckPolicy { get; init; } = "Explicit";
    public string? FilterSubject { get; init; }
    public int MaxDeliver { get; init; } = -1;

    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Consumer;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class CreateConsumerCommandValidator : AbstractValidator<CreateConsumerCommand>
{
    public CreateConsumerCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.StreamName).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
    }
}

public sealed class CreateConsumerCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<CreateConsumerCommand, Unit>
{
    public async Task ExecuteAsync(CreateConsumerCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.CreateConsumerAsync(request, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed record DeleteConsumerCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string StreamName { get; init; }
    public required string Name { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Consumer;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteConsumerCommandValidator : AbstractValidator<DeleteConsumerCommand>
{
    public DeleteConsumerCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.StreamName).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public sealed class DeleteConsumerCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<DeleteConsumerCommand, Unit>
{
    public async Task ExecuteAsync(DeleteConsumerCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.DeleteConsumerAsync(request.EnvironmentId, request.StreamName, request.Name, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

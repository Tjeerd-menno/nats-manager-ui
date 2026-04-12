using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.JetStream.Commands;

public sealed record UpdateStreamCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public long MaxMessages { get; init; } = -1;
    public long MaxBytes { get; init; } = -1;
    public int Replicas { get; init; } = 1;

    ActionType IAuditableCommand.ActionType => ActionType.Update;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Stream;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class UpdateStreamCommandValidator : AbstractValidator<UpdateStreamCommand>
{
    public UpdateStreamCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Subjects).NotEmpty();
    }
}

public sealed class UpdateStreamCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<UpdateStreamCommand, Unit>
{
    public async Task ExecuteAsync(UpdateStreamCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.UpdateStreamAsync(request, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

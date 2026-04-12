using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.JetStream.Commands;

public sealed record CreateStreamCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public string RetentionPolicy { get; init; } = "Limits";
    public string StorageType { get; init; } = "File";
    public long MaxMessages { get; init; } = -1;
    public long MaxBytes { get; init; } = -1;
    public int Replicas { get; init; } = 1;
    public string DiscardPolicy { get; init; } = "Old";

    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Stream;
    string IAuditableCommand.ResourceId => Name;
    string IAuditableCommand.ResourceName => Name;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class CreateStreamCommandValidator : AbstractValidator<CreateStreamCommand>
{
    public CreateStreamCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Subjects).NotEmpty();
        RuleFor(x => x.Replicas).GreaterThan(0);
    }
}

public sealed class CreateStreamCommandHandler(
    IJetStreamWriteAdapter writeAdapter,
    IAuditTrail auditTrail) : IUseCase<CreateStreamCommand, Unit>
{
    public async Task ExecuteAsync(CreateStreamCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await writeAdapter.CreateStreamAsync(request, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

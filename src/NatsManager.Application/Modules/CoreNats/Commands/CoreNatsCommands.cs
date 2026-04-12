using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.CoreNats.Commands;

public sealed class PublishMessageCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? Payload { get; init; }
    ActionType IAuditableCommand.ActionType => ActionType.Publish;
    ResourceType IAuditableCommand.ResourceType => ResourceType.Stream;
    string IAuditableCommand.ResourceId => Subject;
    string IAuditableCommand.ResourceName => Subject;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class PublishMessageCommandValidator : AbstractValidator<PublishMessageCommand>
{
    public PublishMessageCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
    }
}

public sealed class PublishMessageCommandHandler(ICoreNatsAdapter adapter, IAuditTrail auditTrail) : IUseCase<PublishMessageCommand, Unit>
{
    public async Task ExecuteAsync(PublishMessageCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        var data = request.Payload is not null
            ? System.Text.Encoding.UTF8.GetBytes(request.Payload)
            : [];
        await adapter.PublishAsync(request.EnvironmentId, request.Subject, data, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

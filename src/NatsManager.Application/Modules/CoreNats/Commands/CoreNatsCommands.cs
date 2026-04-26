using System.Text.Json;
using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.CoreNats.Models;
using NatsManager.Application.Modules.CoreNats.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.CoreNats.Commands;

public sealed class PublishMessageCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? Payload { get; init; }
    public PayloadFormat PayloadFormat { get; init; } = PayloadFormat.PlainText;
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
    public string? ReplyTo { get; init; }
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

        RuleForEach(x => x.Headers)
            .Must(kvp => !string.IsNullOrWhiteSpace(kvp.Key))
            .WithMessage("Header key must not be empty.");

        When(x => x.PayloadFormat == PayloadFormat.Json && x.Payload != null, () =>
        {
            RuleFor(x => x.Payload)
                .Must(p =>
                {
                    try { JsonDocument.Parse(p!); return true; }
                    catch (System.Text.Json.JsonException) { return false; }
                })
                .WithMessage("Payload is not valid JSON.");
        });

        When(x => x.PayloadFormat == PayloadFormat.HexBytes && x.Payload != null, () =>
        {
            RuleFor(x => x.Payload)
                .Must(p =>
                {
                    try { Convert.FromHexString(p!); return true; }
                    catch (FormatException) { return false; }
                })
                .WithMessage("Payload is not valid hex-encoded bytes.");
        });
    }
}

public sealed class PublishMessageCommandHandler(ICoreNatsAdapter adapter, IAuditTrail auditTrail) : IUseCase<PublishMessageCommand, Unit>
{
    public async Task ExecuteAsync(PublishMessageCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        byte[] data;
        if (request.Payload is null)
        {
            data = [];
        }
        else
        {
            data = request.PayloadFormat switch
            {
                PayloadFormat.HexBytes => Convert.FromHexString(request.Payload),
                _ => System.Text.Encoding.UTF8.GetBytes(request.Payload),
            };
        }

        var headers = request.Headers.Count > 0 ? request.Headers : null;
        await adapter.PublishAsync(request.EnvironmentId, request.Subject, data, headers, request.ReplyTo, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

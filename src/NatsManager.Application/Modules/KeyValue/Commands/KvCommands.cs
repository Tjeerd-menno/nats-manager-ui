using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.KeyValue.Commands;

public sealed record CreateKvBucketCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string BucketName { get; init; }
    public int History { get; init; } = 1;
    public long MaxBytes { get; init; } = -1;
    public int MaxValueSize { get; init; } = -1;
    public TimeSpan? Ttl { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.KvBucket;
    string IAuditableCommand.ResourceId => BucketName;
    string IAuditableCommand.ResourceName => BucketName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class CreateKvBucketCommandValidator : AbstractValidator<CreateKvBucketCommand>
{
    public CreateKvBucketCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.BucketName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.History).GreaterThan(0);
    }
}

public sealed class CreateKvBucketCommandHandler(IKvStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<CreateKvBucketCommand, Unit>
{
    public async Task ExecuteAsync(CreateKvBucketCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.CreateBucketAsync(request.EnvironmentId, request.BucketName, request.History, request.MaxBytes, request.MaxValueSize, request.Ttl, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed record DeleteKvBucketCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string BucketName { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.KvBucket;
    string IAuditableCommand.ResourceId => BucketName;
    string IAuditableCommand.ResourceName => BucketName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteKvBucketCommandValidator : AbstractValidator<DeleteKvBucketCommand>
{
    public DeleteKvBucketCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.BucketName).NotEmpty();
    }
}

public sealed class DeleteKvBucketCommandHandler(IKvStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<DeleteKvBucketCommand, Unit>
{
    public async Task ExecuteAsync(DeleteKvBucketCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.DeleteBucketAsync(request.EnvironmentId, request.BucketName, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed record PutKvKeyCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string BucketName { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public long? ExpectedRevision { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Update;
    ResourceType IAuditableCommand.ResourceType => ResourceType.KvKey;
    string IAuditableCommand.ResourceId => Key;
    string IAuditableCommand.ResourceName => Key;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class PutKvKeyCommandValidator : AbstractValidator<PutKvKeyCommand>
{
    public PutKvKeyCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.BucketName).NotEmpty();
        RuleFor(x => x.Key).NotEmpty();
        RuleFor(x => x.Value).NotNull();
    }
}

public sealed class PutKvKeyCommandHandler(IKvStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<PutKvKeyCommand, long>
{
    public async Task ExecuteAsync(PutKvKeyCommand request, IOutputPort<long> outputPort, CancellationToken cancellationToken)
    {
        var valueBytes = Convert.FromBase64String(request.Value);
        var revision = await adapter.PutKeyAsync(request.EnvironmentId, request.BucketName, request.Key, valueBytes, request.ExpectedRevision, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(revision);
    }
}

public sealed record DeleteKvKeyCommand : IAuditableCommand
{
    public required Guid EnvironmentId { get; init; }
    public required string BucketName { get; init; }
    public required string Key { get; init; }

    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.KvKey;
    string IAuditableCommand.ResourceId => Key;
    string IAuditableCommand.ResourceName => Key;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteKvKeyCommandValidator : AbstractValidator<DeleteKvKeyCommand>
{
    public DeleteKvKeyCommandValidator()
    {
        RuleFor(x => x.EnvironmentId).NotEmpty();
        RuleFor(x => x.BucketName).NotEmpty();
        RuleFor(x => x.Key).NotEmpty();
    }
}

public sealed class DeleteKvKeyCommandHandler(IKvStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<DeleteKvKeyCommand, Unit>
{
    public async Task ExecuteAsync(DeleteKvKeyCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.DeleteKeyAsync(request.EnvironmentId, request.BucketName, request.Key, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

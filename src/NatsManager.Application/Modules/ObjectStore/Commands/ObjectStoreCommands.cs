using FluentValidation;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Application.Modules.ObjectStore.Commands;

public sealed class CreateObjectBucketCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string BucketName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public long? MaxBucketSize { get; init; }
    public long? MaxChunkSize { get; init; }
    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.ObjectBucket;
    string IAuditableCommand.ResourceId => BucketName;
    string IAuditableCommand.ResourceName => BucketName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class CreateObjectBucketCommandValidator : AbstractValidator<CreateObjectBucketCommand>
{
    public CreateObjectBucketCommandValidator()
    {
        RuleFor(x => x.BucketName).NotEmpty().MaximumLength(255);
    }
}

public sealed class CreateObjectBucketCommandHandler(IObjectStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<CreateObjectBucketCommand, Unit>
{
    public async Task ExecuteAsync(CreateObjectBucketCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.CreateBucketAsync(request.EnvironmentId, request.BucketName, request.Description, request.MaxBucketSize, request.MaxChunkSize, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class DeleteObjectBucketCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string BucketName { get; init; } = string.Empty;
    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.ObjectBucket;
    string IAuditableCommand.ResourceId => BucketName;
    string IAuditableCommand.ResourceName => BucketName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteObjectBucketCommandHandler(IObjectStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<DeleteObjectBucketCommand, Unit>
{
    public async Task ExecuteAsync(DeleteObjectBucketCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.DeleteBucketAsync(request.EnvironmentId, request.BucketName, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class UploadObjectCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string BucketName { get; init; } = string.Empty;
    public string ObjectName { get; init; } = string.Empty;
    public byte[] Data { get; init; } = [];
    public string? ContentType { get; init; }
    ActionType IAuditableCommand.ActionType => ActionType.Create;
    ResourceType IAuditableCommand.ResourceType => ResourceType.ObjectItem;
    string IAuditableCommand.ResourceId => $"{BucketName}/{ObjectName}";
    string IAuditableCommand.ResourceName => ObjectName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class UploadObjectCommandValidator : AbstractValidator<UploadObjectCommand>
{
    public UploadObjectCommandValidator()
    {
        RuleFor(x => x.ObjectName).NotEmpty();
        RuleFor(x => x.Data).NotNull();
    }
}

public sealed class UploadObjectCommandHandler(IObjectStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<UploadObjectCommand, Unit>
{
    public async Task ExecuteAsync(UploadObjectCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.UploadObjectAsync(request.EnvironmentId, request.BucketName, request.ObjectName, request.Data, request.ContentType, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

public sealed class DeleteObjectCommand : IAuditableCommand
{
    public Guid EnvironmentId { get; init; }
    public string BucketName { get; init; } = string.Empty;
    public string ObjectName { get; init; } = string.Empty;
    ActionType IAuditableCommand.ActionType => ActionType.Delete;
    ResourceType IAuditableCommand.ResourceType => ResourceType.ObjectItem;
    string IAuditableCommand.ResourceId => $"{BucketName}/{ObjectName}";
    string IAuditableCommand.ResourceName => ObjectName;
    Guid? IAuditableCommand.EnvironmentId => EnvironmentId;
}

public sealed class DeleteObjectCommandHandler(IObjectStoreAdapter adapter, IAuditTrail auditTrail) : IUseCase<DeleteObjectCommand, Unit>
{
    public async Task ExecuteAsync(DeleteObjectCommand request, IOutputPort<Unit> outputPort, CancellationToken cancellationToken)
    {
        await adapter.DeleteObjectAsync(request.EnvironmentId, request.BucketName, request.ObjectName, cancellationToken);
        await auditTrail.RecordAsync(request, cancellationToken);
        outputPort.Success(Unit.Value);
    }
}

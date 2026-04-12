using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.ObjectStore.Commands;
using NatsManager.Application.Modules.ObjectStore.Models;
using NatsManager.Application.Modules.ObjectStore.Ports;
using NatsManager.Application.Modules.ObjectStore.Queries;

namespace NatsManager.Application.Tests.Modules.ObjectStore;

public sealed class GetObjectBucketsQueryTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly GetObjectBucketsQueryHandler _handler;

    public GetObjectBucketsQueryTests()
    {
        _handler = new GetObjectBucketsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnBucketsFromAdapter()
    {
        var envId = Guid.NewGuid();
        var buckets = new List<ObjectBucketInfo>
        {
            new("bucket1", 5, 1024, null)
        };
        _adapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(buckets);

        var outputPort = new TestOutputPort<IReadOnlyList<ObjectBucketInfo>>();
        await _handler.ExecuteAsync(new GetObjectBucketsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].BucketName.Should().Be("bucket1");
    }
}

public sealed class GetObjectBucketDetailQueryTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly GetObjectBucketDetailQueryHandler _handler;

    public GetObjectBucketDetailQueryTests()
    {
        _handler = new GetObjectBucketDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingBucket_ShouldReturn()
    {
        var envId = Guid.NewGuid();
        var bucket = new ObjectBucketInfo("bucket1", 5, 1024, "desc");
        _adapter.GetBucketAsync(envId, "bucket1", Arg.Any<CancellationToken>()).Returns(bucket);

        var outputPort = new TestOutputPort<ObjectBucketInfo>();
        await _handler.ExecuteAsync(new GetObjectBucketDetailQuery(envId, "bucket1"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().NotBeNull();
        outputPort.Value!.BucketName.Should().Be("bucket1");
    }

    [Fact]
    public async Task Handle_WithMissingBucket_ShouldReturnNull()
    {
        var envId = Guid.NewGuid();
        _adapter.GetBucketAsync(envId, "missing", Arg.Any<CancellationToken>()).Returns((ObjectBucketInfo?)null);

        var outputPort = new TestOutputPort<ObjectBucketInfo>();
        await _handler.ExecuteAsync(new GetObjectBucketDetailQuery(envId, "missing"), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class GetObjectsQueryTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly GetObjectsQueryHandler _handler;

    public GetObjectsQueryTests()
    {
        _handler = new GetObjectsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnObjectsFromAdapter()
    {
        var envId = Guid.NewGuid();
        var objects = new List<ObjectInfo>
        {
            new("file.txt", 512, null, "text/plain", DateTimeOffset.UtcNow, 1, null)
        };
        _adapter.ListObjectsAsync(envId, "bucket1", Arg.Any<CancellationToken>()).Returns(objects);

        var outputPort = new TestOutputPort<IReadOnlyList<ObjectInfo>>();
        await _handler.ExecuteAsync(new GetObjectsQuery(envId, "bucket1"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Name.Should().Be("file.txt");
    }
}

public sealed class GetObjectDetailQueryTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly GetObjectDetailQueryHandler _handler;

    public GetObjectDetailQueryTests()
    {
        _handler = new GetObjectDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingObject_ShouldReturn()
    {
        var envId = Guid.NewGuid();
        var obj = new ObjectInfo("file.txt", 512, null, "text/plain", DateTimeOffset.UtcNow, 1, null);
        _adapter.GetObjectInfoAsync(envId, "bucket1", "file.txt", Arg.Any<CancellationToken>()).Returns(obj);

        var outputPort = new TestOutputPort<ObjectInfo>();
        await _handler.ExecuteAsync(new GetObjectDetailQuery(envId, "bucket1", "file.txt"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().NotBeNull();
        outputPort.Value!.Name.Should().Be("file.txt");
    }
}

public sealed class DownloadObjectQueryTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly DownloadObjectQueryHandler _handler;

    public DownloadObjectQueryTests()
    {
        _handler = new DownloadObjectQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnBytes()
    {
        var envId = Guid.NewGuid();
        var data = new byte[] { 1, 2, 3 };
        _adapter.DownloadObjectAsync(envId, "bucket1", "file.bin", Arg.Any<CancellationToken>()).Returns(data);

        var outputPort = new TestOutputPort<byte[]?>();
        await _handler.ExecuteAsync(new DownloadObjectQuery(envId, "bucket1", "file.bin"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task Handle_WhenAdapterThrows_ShouldReturnNull()
    {
        var envId = Guid.NewGuid();
        _adapter.DownloadObjectAsync(envId, "bucket1", "missing", Arg.Any<CancellationToken>())
            .Returns<byte[]>(x => throw new InvalidOperationException("Not found"));

        var outputPort = new TestOutputPort<byte[]?>();
        await _handler.ExecuteAsync(new DownloadObjectQuery(envId, "bucket1", "missing"), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class CreateObjectBucketCommandTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly CreateObjectBucketCommandHandler _handler;

    public CreateObjectBucketCommandTests()
    {
        _handler = new CreateObjectBucketCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var command = new CreateObjectBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "my-bucket",
            Description = "desc",
            MaxBucketSize = 1024,
            MaxChunkSize = 256
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).CreateBucketAsync(
            command.EnvironmentId, "my-bucket", "desc", 1024L, 256L, Arg.Any<CancellationToken>());
    }
}

public sealed class CreateObjectBucketCommandValidatorTests
{
    private readonly CreateObjectBucketCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_WhenBucketNameEmpty()
    {
        var command = new CreateObjectBucketCommand { BucketName = "" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_WhenBucketNameTooLong()
    {
        var command = new CreateObjectBucketCommand { BucketName = new string('a', 256) };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new CreateObjectBucketCommand { BucketName = "valid-bucket" };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

public sealed class UploadObjectCommandTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly UploadObjectCommandHandler _handler;

    public UploadObjectCommandTests()
    {
        _handler = new UploadObjectCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var data = new byte[] { 1, 2, 3 };
        var command = new UploadObjectCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "bucket",
            ObjectName = "file.bin",
            Data = data,
            ContentType = "application/octet-stream"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).UploadObjectAsync(
            command.EnvironmentId, "bucket", "file.bin", data, "application/octet-stream", Arg.Any<CancellationToken>());
    }
}

public sealed class UploadObjectCommandValidatorTests
{
    private readonly UploadObjectCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_WhenObjectNameEmpty()
    {
        var command = new UploadObjectCommand { ObjectName = "", Data = [1] };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Should_Pass_WhenValid()
    {
        var command = new UploadObjectCommand { ObjectName = "file.bin", Data = [1, 2] };
        var result = _validator.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}

public sealed class DeleteObjectCommandTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteObjectCommandHandler _handler;

    public DeleteObjectCommandTests()
    {
        _handler = new DeleteObjectCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var command = new DeleteObjectCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "bucket",
            ObjectName = "file.bin"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).DeleteObjectAsync(
            command.EnvironmentId, "bucket", "file.bin", Arg.Any<CancellationToken>());
    }
}

public sealed class DeleteObjectBucketCommandTests
{
    private readonly IObjectStoreAdapter _adapter = Substitute.For<IObjectStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteObjectBucketCommandHandler _handler;

    public DeleteObjectBucketCommandTests()
    {
        _handler = new DeleteObjectBucketCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var command = new DeleteObjectBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "my-bucket"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).DeleteBucketAsync(
            command.EnvironmentId, "my-bucket", Arg.Any<CancellationToken>());
    }
}

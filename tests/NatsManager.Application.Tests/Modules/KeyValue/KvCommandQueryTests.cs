using FluentAssertions;
using NSubstitute;
using NatsManager.Application.Behaviors;
using NatsManager.Application.Common;
using NatsManager.Application.Modules.KeyValue.Commands;
using NatsManager.Application.Modules.KeyValue.Models;
using NatsManager.Application.Modules.KeyValue.Ports;
using NatsManager.Application.Modules.KeyValue.Queries;

namespace NatsManager.Application.Tests.Modules.KeyValue;

public sealed class GetKvBucketsQueryTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly GetKvBucketsQueryHandler _handler;

    public GetKvBucketsQueryTests()
    {
        _handler = new GetKvBucketsQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnBuckets()
    {
        var envId = Guid.NewGuid();
        var buckets = new List<KvBucketInfo>
        {
            new("config", 5, -1, -1, null, 10, 1024),
            new("cache", 1, 1048576, -1, TimeSpan.FromHours(1), 50, 4096)
        };

        _adapter.ListBucketsAsync(envId, Arg.Any<CancellationToken>()).Returns(buckets);

        var outputPort = new TestOutputPort<IReadOnlyList<KvBucketInfo>>();
        await _handler.ExecuteAsync(new GetKvBucketsQuery(envId), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(2);
        outputPort.Value![0].BucketName.Should().Be("config");
    }
}

public sealed class GetKvKeysQueryTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly GetKvKeysQueryHandler _handler;

    public GetKvKeysQueryTests()
    {
        _handler = new GetKvKeysQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_ShouldReturnKeys()
    {
        var envId = Guid.NewGuid();
        var entries = new List<KvEntry>
        {
            new("key1", "value1", 1, "Put", DateTimeOffset.UtcNow, 6)
        };

        _adapter.ListKeysAsync(envId, "bucket", null, Arg.Any<CancellationToken>()).Returns(entries);

        var outputPort = new TestOutputPort<IReadOnlyList<KvEntry>>();
        await _handler.ExecuteAsync(new GetKvKeysQuery(envId, "bucket", null), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().HaveCount(1);
        outputPort.Value![0].Key.Should().Be("key1");
    }
}

public sealed class GetKvKeyDetailQueryTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly GetKvKeyDetailQueryHandler _handler;

    public GetKvKeyDetailQueryTests()
    {
        _handler = new GetKvKeyDetailQueryHandler(_adapter);
    }

    [Fact]
    public async Task Handle_WithExistingKey_ShouldReturn()
    {
        var envId = Guid.NewGuid();
        var entry = new KvEntry("key1", "value1", 3, "Put", DateTimeOffset.UtcNow, 6);

        _adapter.GetKeyAsync(envId, "bucket", "key1", Arg.Any<CancellationToken>()).Returns(entry);

        var outputPort = new TestOutputPort<KvEntry>();
        await _handler.ExecuteAsync(new GetKvKeyDetailQuery(envId, "bucket", "key1"), outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().NotBeNull();
        outputPort.Value!.Revision.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithNonExistentKey_ShouldReturnNull()
    {
        var envId = Guid.NewGuid();
        _adapter.GetKeyAsync(envId, "bucket", "missing", Arg.Any<CancellationToken>()).Returns((KvEntry?)null);

        var outputPort = new TestOutputPort<KvEntry>();
        await _handler.ExecuteAsync(new GetKvKeyDetailQuery(envId, "bucket", "missing"), outputPort, CancellationToken.None);

        outputPort.IsNotFound.Should().BeTrue();
    }
}

public sealed class CreateKvBucketCommandTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly CreateKvBucketCommandHandler _handler;

    public CreateKvBucketCommandTests()
    {
        _handler = new CreateKvBucketCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var command = new CreateKvBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "test-bucket",
            History = 5
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).CreateBucketAsync(
            command.EnvironmentId, "test-bucket", 5, -1, -1, null, Arg.Any<CancellationToken>());
    }
}

public sealed class CreateKvBucketCommandValidatorTests
{
    private readonly CreateKvBucketCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new CreateKvBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "bucket"
        };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyBucketName_ShouldFail()
    {
        var command = new CreateKvBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = ""
        };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithZeroHistory_ShouldFail()
    {
        var command = new CreateKvBucketCommand
        {
            EnvironmentId = Guid.NewGuid(),
            BucketName = "bucket",
            History = 0
        };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}

public sealed class PutKvKeyCommandTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly PutKvKeyCommandHandler _handler;

    public PutKvKeyCommandTests()
    {
        _handler = new PutKvKeyCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDecodeBase64AndPutKey()
    {
        var envId = Guid.NewGuid();
        var valueBase64 = Convert.ToBase64String("hello"u8.ToArray());
        var command = new PutKvKeyCommand
        {
            EnvironmentId = envId,
            BucketName = "bucket",
            Key = "key1",
            Value = valueBase64
        };

        _adapter.PutKeyAsync(envId, "bucket", "key1", Arg.Any<byte[]>(), null, Arg.Any<CancellationToken>())
            .Returns(1L);

        var outputPort = new TestOutputPort<long>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().Be(1);
        await _adapter.Received(1).PutKeyAsync(
            envId, "bucket", "key1", Arg.Any<byte[]>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExpectedRevision_ShouldPassItToAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new PutKvKeyCommand
        {
            EnvironmentId = envId,
            BucketName = "bucket",
            Key = "key1",
            Value = Convert.ToBase64String("data"u8.ToArray()),
            ExpectedRevision = 5
        };

        _adapter.PutKeyAsync(envId, "bucket", "key1", Arg.Any<byte[]>(), 5L, Arg.Any<CancellationToken>())
            .Returns(6L);

        var outputPort = new TestOutputPort<long>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        outputPort.Value.Should().Be(6);
    }
}

public sealed class DeleteKvKeyCommandTests
{
    private readonly IKvStoreAdapter _adapter = Substitute.For<IKvStoreAdapter>();
    private readonly IAuditTrail _auditTrail = Substitute.For<IAuditTrail>();
    private readonly DeleteKvKeyCommandHandler _handler;

    public DeleteKvKeyCommandTests()
    {
        _handler = new DeleteKvKeyCommandHandler(_adapter, _auditTrail);
    }

    [Fact]
    public async Task Handle_ShouldDelegateToAdapter()
    {
        var envId = Guid.NewGuid();
        var command = new DeleteKvKeyCommand
        {
            EnvironmentId = envId,
            BucketName = "bucket",
            Key = "key1"
        };

        var outputPort = new TestOutputPort<Unit>();
        await _handler.ExecuteAsync(command, outputPort, CancellationToken.None);

        outputPort.IsSuccess.Should().BeTrue();
        await _adapter.Received(1).DeleteKeyAsync(envId, "bucket", "key1", Arg.Any<CancellationToken>());
    }
}

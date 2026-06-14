using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class JetStreamReadWriteAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private JetStreamReadAdapter CreateReadAdapter() => new(ConnectionFactory, NullLogger<JetStreamReadAdapter>());

    private JetStreamWriteAdapter CreateWriteAdapter() => new(ConnectionFactory, NullLogger<JetStreamWriteAdapter>());

    [Fact]
    public async Task CreateStream_ThenListStreams_ShouldContainCreatedStream()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];

        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        var streams = await readAdapter.ListStreamsAsync(EnvironmentId);

        streams.ShouldContain(s => s.Name == streamName);
    }

    [Fact]
    public async Task GetStreamAsync_WithExistingStream_ShouldReturnStreamInfo()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        var info = await readAdapter.GetStreamAsync(EnvironmentId, streamName);

        info.ShouldNotBeNull();
        info!.Name.ShouldBe(streamName);
    }

    [Fact]
    public async Task GetStreamAsync_WithNonExistentStream_ShouldReturnNull()
    {
        var readAdapter = CreateReadAdapter();

        var result = await readAdapter.GetStreamAsync(EnvironmentId, "nonexistent-stream");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetStreamConfigAsync_ShouldReturnConfig()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"],
            RetentionPolicy = "Limits",
            StorageType = "Memory"
        });

        var config = await readAdapter.GetStreamConfigAsync(EnvironmentId, streamName);

        config.ShouldNotBeNull();
        config!.RetentionPolicy.ShouldBe("Limits");
        config.StorageType.ShouldBe("Memory");
    }

    [Fact]
    public async Task DeleteStreamAsync_ShouldRemoveStream()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await writeAdapter.DeleteStreamAsync(EnvironmentId, streamName);

        var info = await readAdapter.GetStreamAsync(EnvironmentId, streamName);
        info.ShouldBeNull();
    }

    [Fact]
    public async Task CreateConsumer_ThenListConsumers_ShouldContainConsumer()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        var consumerName = "test-consumer";

        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await writeAdapter.CreateConsumerAsync(new CreateConsumerSpec
        {
            EnvironmentId = EnvironmentId,
            StreamName = streamName,
            Name = consumerName,
            DeliverPolicy = "All",
            AckPolicy = "Explicit"
        });

        var consumers = await readAdapter.ListConsumersAsync(EnvironmentId, streamName);

        consumers.ShouldContain(c => c.Name == consumerName);
    }

    [Fact]
    public async Task DeleteConsumerAsync_ShouldRemoveConsumer()
    {
        var readAdapter = CreateReadAdapter();
        var writeAdapter = CreateWriteAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        var consumerName = "test-consumer";

        await writeAdapter.CreateStreamAsync(new CreateStreamSpec
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await writeAdapter.CreateConsumerAsync(new CreateConsumerSpec
        {
            EnvironmentId = EnvironmentId,
            StreamName = streamName,
            Name = consumerName,
            DeliverPolicy = "All",
            AckPolicy = "Explicit"
        });

        await writeAdapter.DeleteConsumerAsync(EnvironmentId, streamName, consumerName);

        var consumers = await readAdapter.ListConsumersAsync(EnvironmentId, streamName);
        consumers.ShouldNotContain(c => c.Name == consumerName);
    }
}

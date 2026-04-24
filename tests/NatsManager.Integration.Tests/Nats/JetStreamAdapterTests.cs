using NatsManager.Application.Modules.JetStream.Commands;
using NatsManager.Infrastructure.Nats;
using NatsManager.Integration.Tests.Infrastructure;

namespace NatsManager.Integration.Tests.Nats;

public sealed class JetStreamAdapterTests(NatsFixture fixture) : NatsIntegrationTestBase(fixture)
{
    private JetStreamAdapter CreateAdapter() => new(ConnectionFactory, NullLogger<JetStreamAdapter>());

    [Fact]
    public async Task CreateStream_ThenListStreams_ShouldContainCreatedStream()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];

        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        var streams = await adapter.ListStreamsAsync(EnvironmentId);

        streams.ShouldContain(s => s.Name == streamName);
    }

    [Fact]
    public async Task GetStreamAsync_WithExistingStream_ShouldReturnStreamInfo()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        var info = await adapter.GetStreamAsync(EnvironmentId, streamName);

        info.ShouldNotBeNull();
        info!.Name.ShouldBe(streamName);
    }

    [Fact]
    public async Task GetStreamAsync_WithNonExistentStream_ShouldReturnNull()
    {
        var adapter = CreateAdapter();

        var result = await adapter.GetStreamAsync(EnvironmentId, "nonexistent-stream");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetStreamConfigAsync_ShouldReturnConfig()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"],
            RetentionPolicy = "Limits",
            StorageType = "Memory"
        });

        var config = await adapter.GetStreamConfigAsync(EnvironmentId, streamName);

        config.ShouldNotBeNull();
        config!.RetentionPolicy.ShouldBe("Limits");
        config.StorageType.ShouldBe("Memory");
    }

    [Fact]
    public async Task DeleteStreamAsync_ShouldRemoveStream()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await adapter.DeleteStreamAsync(EnvironmentId, streamName);

        var info = await adapter.GetStreamAsync(EnvironmentId, streamName);
        info.ShouldBeNull();
    }

    [Fact]
    public async Task CreateConsumer_ThenListConsumers_ShouldContainConsumer()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        var consumerName = "test-consumer";

        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await adapter.CreateConsumerAsync(new CreateConsumerCommand
        {
            EnvironmentId = EnvironmentId,
            StreamName = streamName,
            Name = consumerName,
            DeliverPolicy = "All",
            AckPolicy = "Explicit"
        });

        var consumers = await adapter.ListConsumersAsync(EnvironmentId, streamName);

        consumers.ShouldContain(c => c.Name == consumerName);
    }

    [Fact]
    public async Task DeleteConsumerAsync_ShouldRemoveConsumer()
    {
        var adapter = CreateAdapter();
        var streamName = $"test-{Guid.NewGuid():N}"[..20];
        var consumerName = "test-consumer";

        await adapter.CreateStreamAsync(new CreateStreamCommand
        {
            EnvironmentId = EnvironmentId,
            Name = streamName,
            Subjects = [$"{streamName}.>"]
        });

        await adapter.CreateConsumerAsync(new CreateConsumerCommand
        {
            EnvironmentId = EnvironmentId,
            StreamName = streamName,
            Name = consumerName,
            DeliverPolicy = "All",
            AckPolicy = "Explicit"
        });

        await adapter.DeleteConsumerAsync(EnvironmentId, streamName, consumerName);

        var consumers = await adapter.ListConsumersAsync(EnvironmentId, streamName);
        consumers.ShouldNotContain(c => c.Name == consumerName);
    }
}

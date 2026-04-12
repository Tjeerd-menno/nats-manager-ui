using NatsManager.Application.Common;
using NatsManager.Application.Modules.Dashboard.Models;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.JetStream.Ports;
using NatsManager.Application.Modules.KeyValue.Ports;

namespace NatsManager.Application.Modules.Dashboard.Queries;

public sealed record GetDashboardQuery(Guid EnvironmentId);

public sealed class GetDashboardQueryHandler(
    IEnvironmentRepository environmentRepository,
    IJetStreamAdapter jetStreamAdapter,
    IKvStoreAdapter kvStoreAdapter) : IUseCase<GetDashboardQuery, DashboardSummary>
{
    public async Task ExecuteAsync(GetDashboardQuery request, IOutputPort<DashboardSummary> outputPort, CancellationToken cancellationToken)
    {
        var env = await environmentRepository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        var envHealth = new EnvironmentHealth(
            env?.ConnectionStatus.ToString() ?? "Unknown",
            env?.LastSuccessfulContact);

        var alerts = new List<DashboardAlert>();

        // JetStream summary
        int streamCount = 0, consumerCount = 0, unhealthyConsumers = 0;
        long totalMessages = 0, totalBytes = 0;

        try
        {
            var streams = await jetStreamAdapter.ListStreamsAsync(request.EnvironmentId, cancellationToken);
            streamCount = streams.Count;
            totalMessages = streams.Sum(s => s.Messages);
            totalBytes = streams.Sum(s => s.Bytes);

            foreach (var stream in streams)
            {
                var consumers = await jetStreamAdapter.ListConsumersAsync(request.EnvironmentId, stream.Name, cancellationToken);
                consumerCount += consumers.Count;

                foreach (var consumer in consumers)
                {
                    if (!consumer.IsHealthy)
                    {
                        unhealthyConsumers++;
                        alerts.Add(new DashboardAlert("warning", "Consumer", $"{stream.Name}/{consumer.Name}", "Consumer is unhealthy"));
                    }

                    if (consumer.NumPending > 1000)
                    {
                        alerts.Add(new DashboardAlert("warning", "Consumer", $"{stream.Name}/{consumer.Name}", $"High pending count: {consumer.NumPending}"));
                    }
                }
            }
        }
        catch
        {
            alerts.Add(new DashboardAlert("error", "JetStream", "N/A", "Failed to retrieve JetStream data"));
        }

        // KV summary
        int bucketCount = 0;
        long totalKeys = 0;

        try
        {
            var buckets = await kvStoreAdapter.ListBucketsAsync(request.EnvironmentId, cancellationToken);
            bucketCount = buckets.Count;
            totalKeys = buckets.Sum(b => b.KeyCount);
        }
        catch
        {
            alerts.Add(new DashboardAlert("error", "KeyValue", "N/A", "Failed to retrieve KV data"));
        }

        outputPort.Success(new DashboardSummary(
            Environment: envHealth,
            JetStream: new JetStreamSummary(streamCount, consumerCount, unhealthyConsumers, totalMessages, totalBytes),
            KeyValue: new KvSummary(bucketCount, totalKeys),
            Alerts: alerts));
    }
}

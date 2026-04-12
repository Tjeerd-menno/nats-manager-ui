using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NatsManager.Application.Modules.Environments.Ports;
using NatsManager.Application.Modules.Services.Models;
using NatsManager.Application.Modules.Services.Ports;

namespace NatsManager.Infrastructure.Nats;

public sealed partial class ServiceDiscoveryAdapter(
    INatsConnectionFactory connectionFactory,
    ILogger<ServiceDiscoveryAdapter> logger) : IServiceDiscoveryAdapter
{
    public async Task<IReadOnlyList<ServiceInfo>> DiscoverServicesAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var services = new List<ServiceInfo>();

        try
        {
            var inbox = connection.NewInbox();
            await using var sub = await connection.SubscribeCoreAsync<string>(inbox, cancellationToken: cancellationToken);
            await connection.PublishAsync("$SRV.INFO", inbox, cancellationToken: cancellationToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                await foreach (var msg in sub.Msgs.ReadAllAsync(cts.Token))
                {
                    if (msg.Data is not null)
                    {
                        var info = ParseServiceInfo(msg.Data);
                        if (info is not null) services.Add(info);
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout collecting responses — expected
            }
        }
        catch (Exception ex)
        {
            LogServiceDiscoveryError(environmentId, ex);
        }

        return services;
    }

    public async Task<ServiceInfo?> GetServiceAsync(Guid environmentId, string serviceName, CancellationToken cancellationToken = default)
    {
        var services = await DiscoverServicesAsync(environmentId, cancellationToken);
        return services.FirstOrDefault(s => string.Equals(s.Name, serviceName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<string> TestServiceRequestAsync(Guid environmentId, string subject, string? payload, CancellationToken cancellationToken = default)
    {
        var connection = (NatsConnection)await connectionFactory.GetConnectionAsync(environmentId, cancellationToken);
        var data = payload is not null ? Encoding.UTF8.GetBytes(payload) : [];

        var response = await connection.RequestAsync<byte[], byte[]>(subject, data, cancellationToken: cancellationToken);
        return response.Data is not null ? Encoding.UTF8.GetString(response.Data) : string.Empty;
    }

    private static ServiceInfo? ParseServiceInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var endpoints = new List<ServiceEndpoint>();
            if (root.TryGetProperty("endpoints", out var eps))
            {
                foreach (var ep in eps.EnumerateArray())
                {
                    endpoints.Add(new ServiceEndpoint(
                        Name: ep.GetProperty("name").GetString() ?? "",
                        Subject: ep.GetProperty("subject").GetString() ?? "",
                        QueueGroup: ep.TryGetProperty("queue_group", out var qg) ? qg.GetString() : null));
                }
            }

            return new ServiceInfo(
                Name: root.GetProperty("name").GetString() ?? "",
                Id: root.GetProperty("id").GetString() ?? "",
                Version: root.GetProperty("version").GetString() ?? "",
                Description: root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                Endpoints: endpoints,
                Stats: null);
        }
        catch
        {
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to discover services in environment {EnvironmentId}")]
    private partial void LogServiceDiscoveryError(Guid environmentId, Exception ex);
}

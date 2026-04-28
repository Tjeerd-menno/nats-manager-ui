using NatsManager.Application.Modules.CoreNats.Models;

namespace NatsManager.Application.Modules.CoreNats.Ports;

public interface ICoreNatsAdapter
{
    Task<NatsServerInfo?> GetServerInfoAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<ListSubjectsResult> ListSubjectsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NatsClientInfo>> ListClientsAsync(Guid environmentId, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid environmentId, string subject, byte[] data,
        IReadOnlyDictionary<string, string>? headers = null,
        string? replyTo = null,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<NatsLiveMessage> SubscribeAsync(Guid environmentId, string subject, CancellationToken cancellationToken = default);
}

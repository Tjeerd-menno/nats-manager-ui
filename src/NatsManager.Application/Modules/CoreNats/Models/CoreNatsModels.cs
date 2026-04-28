namespace NatsManager.Application.Modules.CoreNats.Models;

public sealed record NatsServerInfo(
    string ServerId,
    string ServerName,
    string Version,
    string Host,
    int Port,
    int MaxPayload,
    int Connections,
    long InMsgs,
    long OutMsgs,
    long InBytes,
    long OutBytes,
    TimeSpan Uptime,
    bool JetStreamEnabled);

public sealed record NatsSubjectInfo(
    string Subject,
    int Subscriptions);

public sealed record NatsClientInfo(
    long Id,
    string Name,
    string? Account,
    string Ip,
    int Port,
    long InMsgs,
    long OutMsgs,
    long InBytes,
    long OutBytes,
    TimeSpan Uptime);

public sealed record ListSubjectsResult(
    IReadOnlyList<NatsSubjectInfo> Subjects,
    bool IsMonitoringAvailable);

public enum PayloadFormat
{
    PlainText,
    Json,
    HexBytes,
}

public sealed record NatsLiveMessage(
    string Subject,
    DateTimeOffset ReceivedAt,
    string PayloadBase64,
    int PayloadSize,
    IReadOnlyDictionary<string, string> Headers,
    string? ReplyTo,
    bool IsBinary);

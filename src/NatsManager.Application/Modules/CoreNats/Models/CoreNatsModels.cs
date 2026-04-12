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

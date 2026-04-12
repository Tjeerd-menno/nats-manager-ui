namespace NatsManager.Application.Modules.JetStream.Models;

public sealed record ConsumerInfo(
    string StreamName,
    string Name,
    string? Description,
    string DeliverPolicy,
    string AckPolicy,
    string? FilterSubject,
    long NumPending,
    long NumAckPending,
    long NumRedelivered,
    bool IsHealthy,
    DateTimeOffset Created,
    ConsumerState State);

public sealed record ConsumerState(
    long Delivered,
    long AckFloor,
    long NumPending,
    long NumAckPending,
    long NumRedelivered);

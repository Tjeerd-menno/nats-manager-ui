namespace NatsManager.Application.Modules.JetStream.Ports;

/// <summary>
/// Port-specific request for creating a JetStream stream. Decouples <see cref="IJetStreamWriteAdapter"/>
/// from the application-layer command/audit types so the adapter depends only on the data it needs.
/// </summary>
public sealed record CreateStreamSpec
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public string RetentionPolicy { get; init; } = "Limits";
    public string StorageType { get; init; } = "File";
    public long MaxMessages { get; init; } = -1;
    public long MaxBytes { get; init; } = -1;
    public int Replicas { get; init; } = 1;
    public string DiscardPolicy { get; init; } = "Old";
}

/// <summary>Port-specific request for updating a JetStream stream.</summary>
public sealed record UpdateStreamSpec
{
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public long MaxMessages { get; init; } = -1;
    public long MaxBytes { get; init; } = -1;
    public int Replicas { get; init; } = 1;
}

/// <summary>Port-specific request for creating a JetStream consumer.</summary>
public sealed record CreateConsumerSpec
{
    public required Guid EnvironmentId { get; init; }
    public required string StreamName { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string DeliverPolicy { get; init; } = "All";
    public string AckPolicy { get; init; } = "Explicit";
    public string? FilterSubject { get; init; }
    public int MaxDeliver { get; init; } = -1;
}

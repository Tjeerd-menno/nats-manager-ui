namespace NatsManager.Application.Modules.JetStream.Models;

public sealed record StreamMessage(
    long Sequence,
    string Subject,
    string? Data,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset Timestamp,
    int Size);

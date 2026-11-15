namespace AndalusiaAcademy.Domain.Models;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string EventKey { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    public string? LastError { get; set; }
}

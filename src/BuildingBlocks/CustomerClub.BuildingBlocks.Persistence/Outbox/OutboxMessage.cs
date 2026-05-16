namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public required string EventType { get; set; }

    public required string EventVersion { get; set; }

    public required string Payload { get; set; }

    public required string ContentType { get; set; } = "application/json";

    public required string Producer { get; set; }

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? PublishedOn { get; set; }

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? LastAttemptOn { get; set; }

    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

}

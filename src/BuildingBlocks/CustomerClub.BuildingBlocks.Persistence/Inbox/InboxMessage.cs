namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; }

    public required Guid EventId { get; set; }

    public required string EventType { get; set; }

    public required string Consumer { get; set; }

    public string Payload { get; set; }

    public string? CorrelationId { get; set; }

    public int RetryCount { get; set; }

    public InboxMessageStatus Status { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset ReceivedOn { get; set; }

    public DateTimeOffset ProcessedOn { get; set; }

    public DateTimeOffset FailedOn { get; set; }
}

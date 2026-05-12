namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; set; }

    public required string EventId { get; set; }

    public required string EventType { get; set; }

    public required string Consumer { get; set; }

    public DateTimeOffset ProcessedOn { get; set; }
}
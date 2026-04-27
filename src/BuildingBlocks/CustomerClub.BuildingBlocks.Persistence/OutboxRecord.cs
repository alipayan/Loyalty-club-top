namespace CustomerClub.BuildingBlocks.Persistence;

public sealed class OutboxRecord
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset? PublishedOnUtc { get; set; }
    public string? CorrelationId { get; set; }
}

namespace CustomerClub.BuildingBlocks.Messaging.Events;

public sealed record EventEnvelope<TPayload>(
    EventMetadata Metadata,
    TPayload Payload)
{
    public Guid EventId => Metadata.EventId;

    public string EventType => Metadata.EventType;

    public string EventVersion => Metadata.EventVersion;

    public DateTimeOffset OccurredOn => Metadata.OccurredOn;

    public string Producer => Metadata.Producer;

    public string? CorrelationId => Metadata.CorrelationId;

    public string? CausationId => Metadata.CausationId;

    public string? TenantOrClubId => Metadata.TenantOrClubId;
}
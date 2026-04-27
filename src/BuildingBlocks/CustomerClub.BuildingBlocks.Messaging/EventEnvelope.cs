namespace CustomerClub.BuildingBlocks.Messaging;

public sealed record EventEnvelope<TPayload>(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTimeOffset OccurredOnUtc,
    string Producer,
    string? CorrelationId,
    string? CausationId,
    string? TenantOrClubId,
    TPayload Payload);

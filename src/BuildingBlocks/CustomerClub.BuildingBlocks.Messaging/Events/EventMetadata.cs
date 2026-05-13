namespace CustomerClub.BuildingBlocks.Messaging.Events;

public sealed record EventMetadata(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTimeOffset OccurredOn,
    string Producer,
    string? CorrelationId = null,
    string? CausationId = null,
    string? TenantOrClubId = null);
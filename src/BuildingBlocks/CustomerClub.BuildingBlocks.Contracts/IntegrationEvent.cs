namespace CustomerClub.BuildingBlocks.Contracts;

public abstract record IntegrationEvent(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTimeOffset OccurredOn,
    string? CorrelationId,
    string? CausationId,
    string Producer,
    string? TenantOrClubId);

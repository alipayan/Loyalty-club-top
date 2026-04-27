namespace CustomerClub.ServiceTemplate.Contracts;

public sealed record SampleCreatedV1(
    Guid EventId,
    string EventType,
    string EventVersion,
    DateTimeOffset OccurredOnUtc,
    string? CorrelationId,
    string? CausationId,
    string Producer,
    string? TenantOrClubId,
    string Name);

namespace CustomerClub.BuildingBlocks.Messaging.Events;

public static class EventHeaders
{
    public const string EventId = "x-event-id";
    public const string EventType = "x-event-type";
    public const string EventVersion = "x-event-version";
    public const string OccurredOn = "x-occurred-on";
    public const string Producer = "x-producer";
    public const string CorrelationId = "x-correlation-id";
    public const string CausationId = "x-causation-id";
    public const string TenantOrClubId = "x-tenant-or-club-id";
    public const string ContentType = "content-type";
}
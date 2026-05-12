namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public sealed class ConsumeContext
{
    public required string ConsumerName { get; init; }

    public required string MessageId { get; init; }

    public string? CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public string? TenantOrClubId { get; init; }

    public int DeliveryAttempt { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
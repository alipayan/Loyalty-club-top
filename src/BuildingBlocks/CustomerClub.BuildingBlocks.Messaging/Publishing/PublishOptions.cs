namespace CustomerClub.BuildingBlocks.Messaging.Publishing;

public sealed class PublishOptions
{
    public string? Topic { get; init; }

    public string? RoutingKey { get; init; }

    public string? CorrelationId { get; init; }

    public string? CausationId { get; init; }

    public string? TenantOrClubId { get; init; }

    public IDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

namespace CustomerClub.BuildingBlocks.Observability;

public static class ObservabilityConventions
{
    public const string ActivitySourcePrefix = "CustomerClub";
    public const string CorrelationHeader = "x-correlation-id";

    public static readonly string[] RequiredLogFields =
    [
        "timestamp",
        "service",
        "environment",
        "traceId",
        "spanId",
        "correlationId",
        "operation",
        "outcome"
    ];
}

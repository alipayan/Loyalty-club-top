namespace CustomerClub.BuildingBlocks.Messaging.Events;

public static class EventConventions
{
    public const string DefaultContentType = "application/json";

    public static string NormalizeEventType(string eventType)
        => eventType.Trim().ToLowerInvariant();

    public static string NormalizeVersion(string version)
        => version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? version.ToLowerInvariant()
            : $"v{version}".ToLowerInvariant();

    public static string BuildRoutingKey(string eventType, string eventVersion)
        => $"{NormalizeEventType(eventType)}.{NormalizeVersion(eventVersion)}";
}
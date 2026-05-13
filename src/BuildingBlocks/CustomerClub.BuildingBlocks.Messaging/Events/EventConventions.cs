namespace CustomerClub.BuildingBlocks.Messaging.Events;

public static class EventConventions
{
    public const string DefaultContentType = "application/json";

    public static string NormalizeEventType(string eventType)
        => eventType.Trim().ToLowerInvariant();

    public static string NormalizeVersion(string version)
        => version.StartsWith('v') ? version : $"v{version}";
}
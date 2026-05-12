namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public sealed record EventHandlingResult(
    bool IsSuccess,
    bool ShouldRetry = false,
    string? Error = null)
{
    public static EventHandlingResult Success()
        => new(true);

    public static EventHandlingResult Retry(string error)
        => new(false, ShouldRetry: true, Error: error);

    public static EventHandlingResult Failure(string error)
        => new(false, ShouldRetry: false, Error: error);
}

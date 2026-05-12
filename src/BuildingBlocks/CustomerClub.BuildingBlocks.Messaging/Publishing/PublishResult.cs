namespace CustomerClub.BuildingBlocks.Messaging.Publishing;

public sealed record PublishResult(
    bool IsSuccess,
    string? MessageId = null,
    string? Error = null)
{
    public static PublishResult Success(string? messageId = null)
        => new(true, messageId);

    public static PublishResult Failure(string error)
        => new(false, Error: error);
}
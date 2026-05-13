namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public interface IInboxStore
{
    Task<bool> HasProcessedAsync(
        string eventId,
        string consumer,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);
}
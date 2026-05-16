namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public interface IInboxStore
{
    Task<bool> HasProcessedAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessedAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);

    Task<InboxMessage> AddAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);
}
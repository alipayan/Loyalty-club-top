namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public interface IInboxStore
{
    Task<bool> HasProcessedAsync(Guid eventId, string consumer, CancellationToken ct);

    Task<bool> TryStartProcessingAsync(
        InboxMessage message,
        CancellationToken ct);

    Task MarkAsProcessedAsync(Guid eventId, string consumer, DateTimeOffset processedOn, CancellationToken ct);

    Task MarkAsFailedAsync(Guid eventId, string consumer, string error, DateTimeOffset failedOn, CancellationToken ct);

    Task<InboxMessage> AddAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default);

}
namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public interface IOutboxStore
{
    Task<OutboxMessage> AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPublishableAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkAsProcessingAsync(
       Guid messageId,
       CancellationToken cancellationToken = default);

    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedOn,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset failedOn,
        int maxRetryCount,
        CancellationToken cancellationToken = default);
}
namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public interface IOutboxStore
{
    Task<OutboxMessage> AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPublishableAsync(
        int batchSize,
        DateTimeOffset now,
        int maxRetryCount,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkAsProcessingAsync(
       Guid messageId,
       DateTimeOffset startedOn,
       DateTimeOffset expiresOn,
       DateTimeOffset lastAttemptOn,
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
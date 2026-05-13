namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public interface IOutboxStore
{
    Task<OutboxMessage> AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkAsProcessingAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedOnUtc,
        CancellationToken cancellationToken = default);

    Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset failedOnUtc,
        CancellationToken cancellationToken = default);
}
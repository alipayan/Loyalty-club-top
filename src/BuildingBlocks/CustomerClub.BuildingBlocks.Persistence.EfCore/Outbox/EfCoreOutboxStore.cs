namespace CustomerClub.BuildingBlocks.Persistence.EfCore.Outbox;

public sealed class EfCoreOutboxStore<TDbContext>(TDbContext dbContext) : IOutboxStore
    where TDbContext : DbContext
{
    private DbSet<OutboxMessage> OutboxMessages => dbContext.Set<OutboxMessage>();

    public async Task<OutboxMessage> AddAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await OutboxMessages.AddAsync(message, cancellationToken);

        return message;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPublishableAsync(
        int batchSize,
        DateTimeOffset now,
        int maxRetryCount,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        return await OutboxMessages
            .AsNoTracking()
            .Where(message =>
                    message.Status == OutboxMessageStatus.Pending ||
                    message.Status == OutboxMessageStatus.Failed ||
                   (message.Status == OutboxMessageStatus.Processing && message.ProcessingExpiresOn <= now) &&
                    message.RetryCount < maxRetryCount
                )
            .OrderBy(message => message.OccurredOn)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryMarkAsProcessingAsync(
        Guid messageId,
        DateTimeOffset startedOn,
        DateTimeOffset expiresOn,
        DateTimeOffset lastAttemptOn,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await OutboxMessages
            .Where(message =>
                message.Id == messageId &&
                (
                    message.Status == OutboxMessageStatus.Pending ||
                    message.Status == OutboxMessageStatus.Failed
                ))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Processing)
                .SetProperty(message => message.LastAttemptOn, lastAttemptOn)
                .SetProperty(message => message.ProcessingStartedOn, startedOn)
                .SetProperty(message => message.ProcessingExpiresOn, expiresOn)
                .SetProperty(message => message.RetryCount, message => message.RetryCount + 1),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task MarkAsPublishedAsync(
        Guid messageId,
        DateTimeOffset publishedOn,
        CancellationToken cancellationToken = default)
    {
        await OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, OutboxMessageStatus.Published)
                .SetProperty(message => message.PublishedOn, publishedOn)
                .SetProperty(message => message.LastError, (string?)null),
                cancellationToken);
    }

    public async Task MarkAsFailedAsync(
        Guid messageId,
        string error,
        DateTimeOffset failedOn,
        int maxRetryCount,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(error))
            error = "Unknown outbox publishing error.";

        var message = await OutboxMessages
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);

        if (message is null)
            return;

        await OutboxMessages
            .Where(message => message.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(message => message.Status, message.RetryCount >= maxRetryCount
            ? OutboxMessageStatus.DeadLettered
            : OutboxMessageStatus.Failed)
                .SetProperty(message => message.LastError, error)
                .SetProperty(message => message.LastAttemptOn, failedOn),
                cancellationToken);
    }
}
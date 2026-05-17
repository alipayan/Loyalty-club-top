namespace CustomerClub.BuildingBlocks.Persistence.EfCore.Inbox;

public sealed class EfCoreInboxStore<TDbContext>(TDbContext dbContext) : IInboxStore
    where TDbContext : DbContext
{
    private DbSet<InboxMessage> InboxMessages => dbContext.Set<InboxMessage>();

    public async Task<bool> HasProcessedAsync(
        Guid eventId,
        string consumer,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(consumer))
            throw new ArgumentException("Consumer name is required.", nameof(consumer));

        return await InboxMessages
            .AsNoTracking()
            .AnyAsync(
                message => message.EventId == eventId && message.Consumer == consumer,
                cancellationToken);
    }

    public async Task<InboxMessage> AddAsync(
        InboxMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await InboxMessages.AddAsync(message, cancellationToken);

        return message;
    }

    public Task MarkAsProcessedAsync(Guid eventId, string consumer, DateTimeOffset processedOn, CancellationToken cancellationToken = default)
    {
        var existingMessage = InboxMessages.Find(eventId);
        if (existingMessage is null)
            return Task.CompletedTask;

        existingMessage.ProcessedOn = DateTimeOffset.Now;
        existingMessage.ProcessedOn = processedOn;
        existingMessage.Status = InboxMessageStatus.Processed;
        return Task.CompletedTask;
    }

    public Task<bool> TryStartProcessingAsync(InboxMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.Status = InboxMessageStatus.Processing;
        message.RetryCount += 1;

        return Task.FromResult(true);
    }

    public Task MarkAsFailedAsync(Guid eventId, string consumer, string error, DateTimeOffset failedOn, CancellationToken ct)
    {
        var existingMessage = InboxMessages.Find(eventId);
        if (existingMessage is null)
            return Task.CompletedTask;

        existingMessage.LastError = error;
        existingMessage.FailedOn = failedOn;
        existingMessage.Status = InboxMessageStatus.Failed;
        return Task.CompletedTask;
    }
}
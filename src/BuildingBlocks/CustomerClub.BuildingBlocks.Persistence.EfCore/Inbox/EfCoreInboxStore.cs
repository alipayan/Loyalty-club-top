namespace CustomerClub.BuildingBlocks.Persistence.EfCore.Inbox;

public sealed class EfCoreInboxStore<TDbContext>(TDbContext dbContext) : IInboxStore
    where TDbContext : DbContext
{
    private DbSet<InboxMessage> InboxMessages => dbContext.Set<InboxMessage>();

    public async Task<bool> HasProcessedAsync(
        string eventId,
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

    public Task MarkAsProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var existingMessage = InboxMessages.Find(message.Id);
        if (existingMessage is null)
            return Task.CompletedTask;

        existingMessage.ProcessedOn = DateTimeOffset.Now;
        return Task.CompletedTask;
    }
}
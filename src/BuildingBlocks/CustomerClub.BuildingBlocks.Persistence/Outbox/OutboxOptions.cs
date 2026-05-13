namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public sealed class OutboxOptions
{
    public int BatchSize { get; set; } = 50;

    public int MaxRetryCount { get; set; } = 5;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
namespace CustomerClub.BuildingBlocks.Persistence.Inbox;

public sealed class InboxOptions
{
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}
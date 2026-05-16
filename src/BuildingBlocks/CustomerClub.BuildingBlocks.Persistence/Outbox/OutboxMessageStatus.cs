namespace CustomerClub.BuildingBlocks.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Published = 2,
    Failed = 3,
    DeadLettered = 4
}
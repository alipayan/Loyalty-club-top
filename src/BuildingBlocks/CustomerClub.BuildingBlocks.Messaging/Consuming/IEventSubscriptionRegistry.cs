namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public interface IEventSubscriptionRegistry
{
    EventSubscription? Resolve(string eventType, string eventVersion);

    IReadOnlyCollection<EventSubscription> GetAll();
}
namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public sealed class EventSubscriptionRegistry(IEnumerable<EventSubscription> subscriptions)
    : IEventSubscriptionRegistry
{
    private readonly IReadOnlyCollection<EventSubscription> _subscriptions =
        [.. subscriptions];

    public EventSubscription? Resolve(string eventType, string eventVersion)
    {
        return _subscriptions.FirstOrDefault(subscription =>
            string.Equals(subscription.EventType, eventType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(subscription.EventVersion, eventVersion, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyCollection<EventSubscription> GetAll()
        => _subscriptions;
}
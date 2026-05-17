using CustomerClub.BuildingBlocks.Messaging.Events;

namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public interface IEventDispatcher
{
    Task<EventHandlingResult> DispatchAsync(
        EventSubscription subscription,
        EventMetadata metadata,
        string rawPayload,
        ConsumeContext context,
        CancellationToken cancellationToken = default);
}
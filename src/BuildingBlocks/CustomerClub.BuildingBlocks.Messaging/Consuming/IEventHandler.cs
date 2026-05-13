using CustomerClub.BuildingBlocks.Messaging.Events;

namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public interface IEventHandler<TPayload>
{
    Task<EventHandlingResult> HandleAsync(
        EventEnvelope<TPayload> envelope,
        ConsumeContext context,
        CancellationToken cancellationToken = default);
}
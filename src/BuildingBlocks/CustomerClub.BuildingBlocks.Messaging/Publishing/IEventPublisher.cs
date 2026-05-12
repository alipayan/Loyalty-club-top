using CustomerClub.BuildingBlocks.Messaging.Events;

namespace CustomerClub.BuildingBlocks.Messaging.Publishing;

public interface IEventPublisher
{
    Task<PublishResult> PublishAsync<TPayload>(
        EventEnvelope<TPayload> envelope,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default);
}
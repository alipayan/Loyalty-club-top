namespace CustomerClub.BuildingBlocks.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TPayload>(EventEnvelope<TPayload> envelope, CancellationToken cancellationToken = default);
}

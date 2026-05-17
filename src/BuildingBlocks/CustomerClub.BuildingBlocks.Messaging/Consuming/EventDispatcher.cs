using CustomerClub.BuildingBlocks.Messaging.Events;
using CustomerClub.BuildingBlocks.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public sealed class EventDispatcher(
    IServiceProvider serviceProvider,
    IEventSerializer serializer)
    : IEventDispatcher
{
    public async Task<EventHandlingResult> DispatchAsync(
        EventSubscription subscription,
        EventMetadata metadata,
        string rawPayload,
        ConsumeContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(metadata);

        var payload = serializer.Deserialize(rawPayload, subscription.PayloadType);

        var envelopeType = typeof(EventEnvelope<>).MakeGenericType(subscription.PayloadType);

        var envelope = Activator.CreateInstance(
            envelopeType,
            metadata,
            payload);

        if (envelope is null)
        {
            return EventHandlingResult.Failure(
                $"Could not create EventEnvelope for event '{metadata.EventType}:{metadata.EventVersion}'.");
        }

        var handler = serviceProvider.GetRequiredService(subscription.HandlerType);

        var handleMethod = subscription.HandlerType.GetMethod(
            nameof(IEventHandler<object>.HandleAsync));

        if (handleMethod is null)
        {
            return EventHandlingResult.Failure(
                $"Handler '{subscription.HandlerType.Name}' does not contain HandleAsync method.");
        }

        var resultTask = handleMethod.Invoke(
            handler,
            new object?[]
            {
                envelope,
                context,
                cancellationToken
            }) as Task<EventHandlingResult>;

        if (resultTask is null)
        {
            return EventHandlingResult.Failure(
                $"Handler '{subscription.HandlerType.Name}' returned invalid result.");
        }

        return await resultTask;
    }
}
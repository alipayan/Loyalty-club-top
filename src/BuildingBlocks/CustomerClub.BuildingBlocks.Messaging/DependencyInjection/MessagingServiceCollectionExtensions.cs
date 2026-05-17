using CustomerClub.BuildingBlocks.Messaging.Consuming;
using CustomerClub.BuildingBlocks.Messaging.Events;
using CustomerClub.BuildingBlocks.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CustomerClub.BuildingBlocks.Messaging.DependencyInjection;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerClubMessagingCore(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IEventSerializer, SystemTextJsonEventSerializer>();
        services.TryAddSingleton<IEventSubscriptionRegistry, EventSubscriptionRegistry>();
        services.TryAddScoped<IEventDispatcher, EventDispatcher>();

        return services;
    }

    public static IServiceCollection AddEventSubscription<TPayload, THandler>(
        this IServiceCollection services,
        string eventType,
        string eventVersion,
        string routingKey,
        string queueName,
        string consumerName)
        where THandler : class, IEventHandler<TPayload>
    {
        services.AddCustomerClubMessagingCore();

        services.AddScoped<THandler>();

        services.AddSingleton(new EventSubscription(
            EventType: EventConventions.NormalizeEventType(eventType),
            EventVersion: EventConventions.NormalizeVersion(eventVersion),
            RoutingKey: routingKey,
            QueueName: queueName,
            ConsumerName: consumerName,
            PayloadType: typeof(TPayload),
            HandlerType: typeof(THandler)));

        return services;
    }
}
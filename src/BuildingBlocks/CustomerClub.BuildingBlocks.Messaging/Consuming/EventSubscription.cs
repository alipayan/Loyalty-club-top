namespace CustomerClub.BuildingBlocks.Messaging.Consuming;

public sealed record EventSubscription(
    string EventType,
    string EventVersion,
    string RoutingKey,
    string QueueName,
    string ConsumerName,
    Type PayloadType,
    Type HandlerType);
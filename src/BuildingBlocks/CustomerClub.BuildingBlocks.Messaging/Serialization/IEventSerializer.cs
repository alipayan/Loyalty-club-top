namespace CustomerClub.BuildingBlocks.Messaging.Serialization;

public interface IEventSerializer
{
    string ContentType { get; }

    string Serialize<TPayload>(TPayload payload);

    string Serialize(object payload, Type payloadType);

    TPayload Deserialize<TPayload>(string payload);

    object Deserialize(string payload, Type payloadType);
}
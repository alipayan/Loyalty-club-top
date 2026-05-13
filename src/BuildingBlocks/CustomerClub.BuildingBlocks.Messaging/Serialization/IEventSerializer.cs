namespace CustomerClub.BuildingBlocks.Messaging.Serialization;

public interface IEventSerializer
{
    string ContentType { get; }

    string Serialize<TPayload>(TPayload payload);

    TPayload Deserialize<TPayload>(string payload);
}
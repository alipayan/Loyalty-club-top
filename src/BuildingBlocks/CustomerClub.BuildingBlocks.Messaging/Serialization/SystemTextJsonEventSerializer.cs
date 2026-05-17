using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomerClub.BuildingBlocks.Messaging.Serialization;

public sealed class SystemTextJsonEventSerializer : IEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string ContentType => "application/json";

    public string Serialize<TPayload>(TPayload payload)
        => JsonSerializer.Serialize(payload, Options);

    public string Serialize(object payload, Type payloadType)
        => JsonSerializer.Serialize(payload, payloadType, Options);

    public TPayload Deserialize<TPayload>(string payload)
        => JsonSerializer.Deserialize<TPayload>(payload, Options)
           ?? throw new InvalidOperationException("Message payload could not be deserialized.");

    public object Deserialize(string payload, Type payloadType)
        => JsonSerializer.Deserialize(payload, payloadType, Options)
           ?? throw new InvalidOperationException("Message payload could not be deserialized.");
}
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

    public TPayload Deserialize<TPayload>(string payload)
        => JsonSerializer.Deserialize<TPayload>(payload, Options)
           ?? throw new Exception("Message payload could not be deserialized.");
}
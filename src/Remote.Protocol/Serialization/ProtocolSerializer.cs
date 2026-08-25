using System.Text.Json;

namespace Remote.Protocol.Serialization;

public static class ProtocolSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] Serialize<T>(T message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, Options);
    }

    public static T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        return JsonSerializer.Deserialize<T>(data, Options);
    }

    public static T? Deserialize<T>(JsonElement element)
    {
        return element.Deserialize<T>(Options);
    }

}
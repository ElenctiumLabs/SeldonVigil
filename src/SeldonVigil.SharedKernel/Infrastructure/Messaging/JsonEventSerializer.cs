using Confluent.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeldonVigil.SharedKernel.Infrastructure.Messaging;

public sealed class JsonEventSerializer<T> : ISerializer<T>, IDeserializer<T>
    where T : class
{
    // Explicit converters: .NET 10 only maps string enum names when configured, and
    // stable names (not ordinals) matter for cross-service message compatibility.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, SerializerOptions);
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return null!;
        }

        return JsonSerializer.Deserialize<T>(data, SerializerOptions) ?? throw new InvalidOperationException("Failed to deserialize message");
    }
}

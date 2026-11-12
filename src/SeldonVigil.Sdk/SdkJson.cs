using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeldonVigil.Sdk;

/// <summary>
/// Shared JSON serializer options for the SDK. Mirrors the API's wire contract:
/// camelCase properties and string enum names.
/// </summary>
internal static class SdkJson
{
    /// <summary>
    /// Reads GUID-bearing ids that the API serializes either as plain strings or as
    /// value-object wrappers (<c>{"value":"..."}</c>) and writes plain strings.
    /// </summary>
    public sealed class WireIdConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return doc.RootElement.TryGetProperty("value", out var value)
                    ? value.GetString()
                    : throw new JsonException("Expected a 'value' property for wrapped identifier.");
            }

            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }

    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new WireIdConverter() }
    };
}

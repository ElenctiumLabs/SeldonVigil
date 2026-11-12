using Confluent.Kafka;
using System.Text.Json;
using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Messaging;

public class JsonEventSerializerTests
{
    private enum SampleLevel
    {
        Low,
        Medium,
        High
    }

    private sealed record SampleEvent(string Name, SampleLevel Level);

    private static byte[] Serialize(SampleEvent evt)
    {
        var serializer = new JsonEventSerializer<SampleEvent>();
        var context = SerializationContextHelper.Create();
        return serializer.Serialize(evt, context);
    }

    private static SampleEvent? Deserialize(byte[] payload)
    {
        var serializer = new JsonEventSerializer<SampleEvent>();
        var context = SerializationContextHelper.Create();
        return serializer.Deserialize(payload, isNull: false, context);
    }

    [Fact]
    public void RoundTripPreservesEnumNames()
    {
        var original = new SampleEvent("probe", SampleLevel.High);

        var bytes = Serialize(original);
        // Enum must travel as a stable name, not an ordinal.
        JsonDocument.Parse(bytes).RootElement.GetProperty("Level").GetString().Should().Be("High");

        var restored = Deserialize(bytes);
        restored!.Name.Should().Be("probe");
        restored.Level.Should().Be(SampleLevel.High);
    }

    [Fact]
    public void DeserializeStillAcceptsNumericEnumPayloads()
    {
        // Messages produced before the string-enum switch remain readable.
        var legacy = JsonSerializer.SerializeToUtf8Bytes(
            new { name = "legacy", level = 2 });

        var restored = Deserialize(legacy);
        restored!.Level.Should().Be(SampleLevel.High);
    }

    [Fact]
    public void DeserializeReturnsNullForNullPayload()
    {
        var serializer = new JsonEventSerializer<SampleEvent>();
        var context = SerializationContextHelper.Create();

        var result = serializer.Deserialize(ReadOnlySpan<byte>.Empty, isNull: true, context);

        result.Should().BeNull();
    }
}

/// <summary>Minimal Confluent.Kafka context so serializers can run without a broker.</summary>
file static class SerializationContextHelper
{
    public static SerializationContext Create() =>
        new(MessageComponentType.Value, "tests");
}

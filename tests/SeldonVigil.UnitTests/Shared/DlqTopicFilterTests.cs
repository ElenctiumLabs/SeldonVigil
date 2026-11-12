using SeldonVigil.SharedKernel.Infrastructure.Messaging;
using Xunit;
using FluentAssertions;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Messaging;

public class DlqTopicFilterTests
{
    [Theory]
    [InlineData("TraceIngestedEvent.dlq")]          // configured default suffix
    [InlineData("traceingestedevent.DLQ")]          // case-insensitive
    [InlineData("TraceIngestedEvent-dlq")]          // legacy dash convention
    [InlineData("security.events.dlq")]
    public void IsDlqWithKnownSuffixesReturnsTrue(string topic)
    {
        DlqTopicFilter.IsDlq(topic).Should().BeTrue();
    }

    [Theory]
    [InlineData("TraceIngestedEvent")]
    [InlineData("traces.raw")]
    [InlineData("__consumer_offsets")]
    [InlineData("audit.events")]
    public void IsDlqWithNonDlqTopicsReturnsFalse(string topic)
    {
        DlqTopicFilter.IsDlq(topic).Should().BeFalse();
    }

    [Fact]
    public void IsDlqHonoursConfiguredSuffix()
    {
        DlqTopicFilter.IsDlq("TraceIngestedEvent.dead", ".dead").Should().BeTrue();
        DlqTopicFilter.IsDlq("TraceIngestedEvent", ".dead").Should().BeFalse();
    }

    [Fact]
    public void IsDlqStillMatchesWellKnownSuffixesWhenCustomSuffixConfigured()
    {
        DlqTopicFilter.IsDlq("TraceIngestedEvent.dlq", ".dead").Should().BeTrue();
        DlqTopicFilter.IsDlq("TraceIngestedEvent-dlq", ".dead").Should().BeTrue();
    }

    [Fact]
    public void IsDlqWithNullConfiguredSuffixStillMatchesDefaults()
    {
        DlqTopicFilter.IsDlq("TraceIngestedEvent.dlq", null).Should().BeTrue();
        DlqTopicFilter.IsDlq("TraceIngestedEvent-dlq", null).Should().BeTrue();
        DlqTopicFilter.IsDlq("TraceIngestedEvent", null).Should().BeFalse();
    }

    [Theory]
    [InlineData("localhost:9092", "127.0.0.1:9092")]
    [InlineData("LOCALHOST:9092", "127.0.0.1:9092")]
    [InlineData("localhost:9092,other-host:9093", "127.0.0.1:9092,other-host:9093")]
    [InlineData("broker1:9092, localhost:9092", "broker1:9092,127.0.0.1:9092")]
    [InlineData("127.0.0.1:9092", "127.0.0.1:9092")]
    [InlineData("broker.internal:9092", "broker.internal:9092")]
    public void NormalizeReplacesLocalhostWithIpv4Loopback(string input, string expected)
    {
        BrokerBootstrapServers.Normalize(input).Should().Be(expected);
    }
}

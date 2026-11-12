using FluentAssertions;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;
using Xunit;

namespace SeldonVigil.UnitTests.Modules.Domain;

public class CostTrackerTests
{
    [Fact]
    public void CostUsageSnapshotRecordedAtIsRecent()
    {
        // Arrange & Act
        var before = DateTime.UtcNow;
        var snapshot = new CostUsageSnapshot(
            Guid.NewGuid(), 100, 50, "gpt-4", DateTime.UtcNow);
        var after = DateTime.UtcNow;

        // Assert
        snapshot.RecordedAt.Should().BeOnOrAfter(before);
        snapshot.RecordedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CostAggregateDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var aggregate = new CostAggregate("gpt-4", 1000, 500, 25, DateTime.MinValue, DateTime.MaxValue);

        // Assert
        aggregate.ModelId.Should().Be("gpt-4");
        aggregate.TotalInputTokens.Should().Be(1000);
        aggregate.TotalOutputTokens.Should().Be(500);
        aggregate.TotalCalls.Should().Be(25);
    }
}

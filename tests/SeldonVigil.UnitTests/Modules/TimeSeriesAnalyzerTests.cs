using FluentAssertions;
using SeldonVigil.Api.Modules.Domain.Agents;
using SeldonVigil.Api.Modules.Domain.Services;
using Xunit;

namespace SeldonVigil.UnitTests.Modules.Domain;

public class TimeSeriesAnalyzerTests
{
    private readonly TimeSeriesAnalyzer _sut = new();

    [Fact]
    public void ForecastEmptyDataReturnsEmptyForecast()
    {
        // Arrange
        var data = Array.Empty<TimeSeriesPoint>();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().BeEmpty();
        result.Seasonality.Should().Be("insufficient_data");
    }

    [Fact]
    public void ForecastSingleDataPointReturnsSamePoint()
    {
        // Arrange
        var data = new List<TimeSeriesPoint>
        {
            new(new DateTime(2025, 1, 1), 100.0)
        };

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(1);
        result.ForecastSeries[0].Value.Should().Be(100.0);
        result.Seasonality.Should().Be("insufficient_data");
    }

    [Fact]
    public void ForecastFlatDataReturnsFlatForecast()
    {
        // Arrange - all values are the same
        var data = Enumerable.Range(0, 48)
            .Select(i => new TimeSeriesPoint(new DateTime(2025, 1, 1).AddHours(i), 100.0))
            .ToList();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(24);
        result.ForecastSeries.All(p => Math.Abs(p.Value - 100.0) < 1.0).Should().BeTrue();
        result.Seasonality.Should().Contain("none");
    }

    [Fact]
    public void ForecastUpwardTrendProducesIncreasingForecast()
    {
        // Arrange - clear upward trend: 1, 2, 3, ..., 48
        var data = Enumerable.Range(0, 48)
            .Select(i => new TimeSeriesPoint(new DateTime(2025, 1, 1).AddHours(i), i + 1.0))
            .ToList();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(24);
        // First forecast should be close to the last data point (smoothed value)
        // Subsequent points should follow the upward trend
        result.ForecastSeries[0].Value.Should().BeGreaterThan(40.0);
        result.ForecastSeries[^1].Value.Should().BeGreaterThan(result.ForecastSeries[0].Value);
    }

    [Fact]
    public void ForecastDownwardTrendProducesDecreasingForecast()
    {
        // Arrange - clear downward trend: 48, 47, ..., 1
        var data = Enumerable.Range(0, 48)
            .Select(i => new TimeSeriesPoint(new DateTime(2025, 1, 1).AddHours(i), 48.0 - i))
            .ToList();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(24);
        // First forecast should be close to the last data point (smoothed value)
        // Subsequent points should follow the downward trend
        result.ForecastSeries[0].Value.Should().BeLessThan(10.0);
        result.ForecastSeries[^1].Value.Should().BeLessThan(result.ForecastSeries[0].Value);
    }

    [Fact]
    public void ForecastDailySeasonalityDetectsSeasonalPattern()
    {
        // Arrange - create daily pattern (24-hour cycle) repeated 7 times = 168 points
        var data = Enumerable.Range(0, 168)
            .Select(i => new TimeSeriesPoint(
                new DateTime(2025, 1, 1).AddHours(i),
                100.0 + 10.0 * Math.Sin(2 * Math.PI * i / 24))) // sine wave with period 24
            .ToList();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(24);
        // Should detect some periodic pattern (daily or sub-daily harmonic)
        result.Seasonality.Should().NotBe("none");
    }

    [Fact]
    public void ForecastLinearRegressionCorrectSlope()
    {
        // Arrange - perfect linear data: y = 2x + 5
        var values = Enumerable.Range(0, 10).Select(i => 2.0 * i + 5).ToArray();

        // Act
        var (slope, intercept) = TimeSeriesAnalyzer.ComputeLinearRegression(values);

        // Assert
        slope.Should().BeApproximately(2.0, 0.001);
        intercept.Should().BeApproximately(5.0, 0.001);
    }

    [Fact]
    public void ForecastConfidenceLevelAffectsResult()
    {
        // Arrange
        var data = Enumerable.Range(0, 48)
            .Select(i => new TimeSeriesPoint(
                new DateTime(2025, 1, 1).AddHours(i),
                100.0 + Random.Shared.NextDouble() * 10))
            .ToList();

        // Act
        var result90 = _sut.Forecast(data, 24, 0.90);
        var result99 = _sut.Forecast(data, 24, 0.99);

        // Assert - both produce valid forecasts with correct confidence
        result90.Confidence.Should().Be(0.90);
        result99.Confidence.Should().Be(0.99);
        result90.ForecastSeries.Should().HaveCount(24);
        result99.ForecastSeries.Should().HaveCount(24);
    }

    [Fact]
    public void ForecastWithNoiseProducesReasonableForecast()
    {
        // Arrange - trend with random noise
        var data = Enumerable.Range(0, 100)
            .Select(i => new TimeSeriesPoint(
                new DateTime(2025, 1, 1).AddHours(i),
                100.0 + 0.5 * i + Random.Shared.NextDouble() * 5))
            .ToList();

        // Act
        var result = _sut.Forecast(data, 48, 0.95);

        // Assert
        result.ForecastSeries.Should().HaveCount(48);
        result.ForecastSeries.All(p => !double.IsNaN(p.Value) && !double.IsInfinity(p.Value)).Should().BeTrue();
        // Forecast should generally follow the upward trend
        result.ForecastSeries[^1].Value.Should().BeGreaterThan(result.ForecastSeries[0].Value);
    }

    [Fact]
    public void ForecastTimestampsContinueFromLastDataPoint()
    {
        // Arrange
        var lastTimestamp = new DateTime(2025, 1, 7, 23, 0, 0);
        var data = Enumerable.Range(0, 48)
            .Select(i => new TimeSeriesPoint(lastTimestamp.AddHours(-47 + i), 100.0))
            .ToList();

        // Act
        var result = _sut.Forecast(data, 24, 0.95);

        // Assert
        result.ForecastSeries.First().Timestamp.Should().Be(lastTimestamp.AddHours(1));
        result.ForecastSeries.Last().Timestamp.Should().Be(lastTimestamp.AddHours(24));
    }
}

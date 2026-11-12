using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using Xunit;

namespace SeldonVigil.UnitTests.Modules.Domain.Infrastructure.Persistence;

public class PostgresEvaluationRepositoryTests
{
    private readonly SeldonVigilDbContext _dbContext;
    private readonly PostgresEvaluationRepository _repository;

    public PostgresEvaluationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SeldonVigilDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenantContext = new Mock<ITenantContext>();
        _dbContext = new SeldonVigilDbContext(options, tenantContext.Object);
        _repository = new PostgresEvaluationRepository(_dbContext);
    }

    [Fact]
    public async Task StoreThenGetRecentReturnsResultsNewestFirst()
    {
        // Arrange
        await _repository.StoreAsync(
            "greeting", new { text = "hi" }, new { text = "hello" },
            NewResult(0.7, DateTime.UtcNow.AddMinutes(-1)));

        await _repository.StoreAsync(
            "greeting", new { text = "hi" }, new { text = "hello" },
            NewResult(0.9, DateTime.UtcNow));

        // Act
        var results = await _repository.GetRecentEvaluationsAsync("greeting", 10);

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].OverallScore.Should().Be(0.9);
        _ = results[0].Metrics.Should().ContainSingle();
        _ = results[0].Metrics[0].MetricName.Should().Be("relevance");
    }

    [Fact]
    public async Task GetRecentRespectsLimit()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await _repository.StoreAsync(
                "greeting", new { }, new { },
                NewResult(0.5, DateTime.UtcNow.AddMinutes(-i)));
        }

        // Act
        var results = await _repository.GetRecentEvaluationsAsync("greeting", 2);

        // Assert
        _ = results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentFiltersByPromptName()
    {
        // Arrange
        await _repository.StoreAsync("greeting", new { }, new { }, NewResult(0.5, DateTime.UtcNow));
        await _repository.StoreAsync("farewell", new { }, new { }, NewResult(0.6, DateTime.UtcNow));

        // Act
        var results = await _repository.GetRecentEvaluationsAsync("greeting", 10);

        // Assert
        _ = results.Should().HaveCount(1);
        _ = results[0].OverallScore.Should().Be(0.5);
    }

    private static EvaluationResult NewResult(double score, DateTime evaluatedAt) => new()
    {
        OverallScore = score,
        Passed = score >= 0.6,
        Metrics = [new MetricScore("relevance", score, "on topic")],
        EvaluationDurationMs = 12,
        EvaluatedAt = evaluatedAt,
    };
}

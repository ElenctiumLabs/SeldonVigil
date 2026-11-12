using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using Xunit;

namespace SeldonVigil.UnitTests.Modules.Domain.Infrastructure.Persistence;

public class PostgresPromptRepositoryTests
{
    private readonly SeldonVigilDbContext _dbContext;
    private readonly PostgresPromptRepository _repository;

    public PostgresPromptRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SeldonVigilDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenantContext = new Mock<ITenantContext>();
        _dbContext = new SeldonVigilDbContext(options, tenantContext.Object);
        _repository = new PostgresPromptRepository(_dbContext);
    }

    [Fact]
    public async Task AddThenGetLatestReturnsTheVersion()
    {
        // Arrange
        var version = NewVersion(1, PromptStatus.Draft);

        // Act
        await _repository.AddAsync(version);

        // Assert
        var result = await _repository.GetLatestVersionAsync("greeting");
        _ = result.Should().NotBeNull();
        _ = result!.Version.Should().Be(1);
        _ = result.Template.Should().Be("Hello {name}");
        _ = result.SystemPrompt.Should().Be("Be helpful.");
    }

    [Fact]
    public async Task GetActiveReturnsOnlyTheActiveVersion()
    {
        // Arrange
        await _repository.AddAsync(NewVersion(1, PromptStatus.Deprecated));
        await _repository.AddAsync(NewVersion(2, PromptStatus.Active));

        // Act
        var result = await _repository.GetActiveVersionAsync("greeting");

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Version.Should().Be(2);
    }

    [Fact]
    public async Task ActivateVersionMovesTheActiveFlag()
    {
        // Arrange
        await _repository.AddAsync(NewVersion(1, PromptStatus.Active));
        await _repository.AddAsync(NewVersion(2, PromptStatus.Draft));

        // Act
        await _repository.ActivateVersionAsync("greeting", 2);

        // Assert
        var active = await _repository.GetActiveVersionAsync("greeting");
        _ = active.Should().NotBeNull();
        _ = active!.Version.Should().Be(2);
        var latest = await _repository.GetLatestVersionAsync("greeting");
        _ = latest!.Status.Should().Be(PromptStatus.Active);
    }

    [Fact]
    public async Task GetLatestReturnsNullForUnknownPrompt()
    {
        var result = await _repository.GetLatestVersionAsync("missing");
        _ = result.Should().BeNull();
    }

    private static PromptVersion NewVersion(int number, PromptStatus status) => new()
    {
        Name = "greeting",
        Version = number,
        Template = "Hello {name}",
        SystemPrompt = "Be helpful.",
        ModelConfig = new ModelConfiguration { DefaultModel = "gpt-4o" },
        Status = status,
        CreatedBy = Guid.NewGuid(),
        CreatedAt = DateTime.UtcNow,
    };
}

using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SeldonVigil.Shared.Domain.Saga;
using SeldonVigil.Shared.Infrastructure.Saga;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Saga;

public class PostgresSagaRepositoryTests
{
    private readonly SeldonVigilDbContext _dbContext;
    private readonly PostgresSagaRepository _repository;

    public PostgresSagaRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<SeldonVigilDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var tenantContext = new Mock<SeldonVigil.Api.Shared.Infrastructure.Tenancy.ITenantContext>();
        _dbContext = new SeldonVigilDbContext(options, tenantContext.Object);
        _repository = new PostgresSagaRepository(_dbContext, new Mock<ILogger<PostgresSagaRepository>>().Object);
    }

    [Fact]
    public async Task SaveAsyncCreatesNewSagaInstance()
    {
        // Arrange
        var saga = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "TestSaga",
            CorrelationId = "corr-123",
            State = SagaState.InProgress,
            CurrentStep = 0,
            TotalSteps = 2,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // Act
        await _repository.SaveAsync(saga, CancellationToken.None);

        // Assert
        var result = await _repository.GetByIdAsync(saga.Id, CancellationToken.None);
        _ = result.Should().NotBeNull();
        _ = result!.SagaType.Should().Be("TestSaga");
        _ = result.State.Should().Be(SagaState.InProgress);
    }

    [Fact]
    public async Task SaveAsyncUpdatesExistingSagaInstance()
    {
        // Arrange
        var saga = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "TestSaga",
            CorrelationId = "corr-123",
            State = SagaState.InProgress,
            CurrentStep = 0,
            TotalSteps = 2,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.SaveAsync(saga, CancellationToken.None);

        // Act
        saga.State = SagaState.Completed;
        saga.CurrentStep = 2;
        saga.CompletedAt = DateTime.UtcNow;
        await _repository.SaveAsync(saga, CancellationToken.None);

        // Assert
        var result = await _repository.GetByIdAsync(saga.Id, CancellationToken.None);
        _ = result.Should().NotBeNull();
        _ = result!.State.Should().Be(SagaState.Completed);
        _ = result.CurrentStep.Should().Be(2);
        _ = result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByCorrelationIdAsyncReturnsMatchingSagas()
    {
        // Arrange
        var correlationId = "corr-456";
        var saga1 = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "Saga1",
            CorrelationId = correlationId,
            State = SagaState.InProgress,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var saga2 = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "Saga2",
            CorrelationId = correlationId,
            State = SagaState.Completed,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.SaveAsync(saga1, CancellationToken.None);
        await _repository.SaveAsync(saga2, CancellationToken.None);

        // Act
        var results = await _repository.GetByCorrelationIdAsync(correlationId, CancellationToken.None);

        // Assert
        _ = results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByStateAsyncReturnsMatchingSagas()
    {
        // Arrange
        var saga = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "TestSaga",
            CorrelationId = "corr-789",
            State = SagaState.Failed,
            RetryCount = 0,
            MaxRetries = 3,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.SaveAsync(saga, CancellationToken.None);

        // Act
        var failedSagas = await _repository.GetByStateAsync(SagaState.Failed, CancellationToken.None);
        var completedSagas = await _repository.GetByStateAsync(SagaState.Completed, CancellationToken.None);

        // Assert
        _ = failedSagas.Should().HaveCount(1);
        _ = completedSagas.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingRetryAsyncReturnsRetryableSagas()
    {
        // Arrange
        var retryableSaga = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "TestSaga",
            CorrelationId = "corr-retry",
            State = SagaState.Failed,
            RetryCount = 1,
            MaxRetries = 3,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var exhaustedSaga = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = "TestSaga",
            CorrelationId = "corr-exhausted",
            State = SagaState.Failed,
            RetryCount = 3,
            MaxRetries = 3,
            SagaData = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _repository.SaveAsync(retryableSaga, CancellationToken.None);
        await _repository.SaveAsync(exhaustedSaga, CancellationToken.None);

        // Act
        var pendingRetry = await _repository.GetPendingRetryAsync(CancellationToken.None);

        // Assert
        _ = pendingRetry.Should().HaveCount(1);
        _ = pendingRetry[0].Id.Should().Be(retryableSaga.Id);
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SeldonVigil.Shared.Domain.Saga;
using SeldonVigil.Shared.Infrastructure.Saga;

namespace SeldonVigil.UnitTests.Shared.Infrastructure.Saga;

public class SagaOrchestratorTests
{
    private readonly Mock<ISagaRepository> _repository = new();
    private readonly Mock<ILogger<SagaOrchestrator>> _logger = new();

    [Fact]
    public async Task CompensateAsyncRunsExecutedStepsInReverseOrder()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaInstance
        {
            Id = sagaId,
            SagaType = "TestSaga",
            State = SagaState.Failed,
            CurrentStep = 2,
            SagaData = """{"value":1}""",
        };

        _repository.Setup(r => r.GetByIdAsync(sagaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);

        var callOrder = new List<string>();
        var first = new FakeSagaStep("First", 1, callOrder);
        var second = new FakeSagaStep("Second", 2, callOrder);
        var orchestrator = CreateOrchestrator(new FakeSagaDefinition(
            "TestSaga", typeof(TestSagaData), new List<ISagaStep> { first, second }));

        // Act
        await orchestrator.CompensateAsync(sagaId);

        // Assert
        first.Compensations.Should().Be(1);
        second.Compensations.Should().Be(1);
        callOrder.Should().Equal("Second", "First");
        saga.State.Should().Be(SagaState.Compensated);
    }

    [Fact]
    public async Task CompensateAsyncOnlyRunsStepsThatAlreadyExecuted()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaInstance
        {
            Id = sagaId,
            SagaType = "TestSaga",
            State = SagaState.Failed,
            CurrentStep = 1,
            SagaData = """{"value":1}""",
        };

        _repository.Setup(r => r.GetByIdAsync(sagaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);

        var first = new FakeSagaStep("First", 1, new List<string>());
        var second = new FakeSagaStep("Second", 2, new List<string>());
        var orchestrator = CreateOrchestrator(new FakeSagaDefinition(
            "TestSaga", typeof(TestSagaData), new List<ISagaStep> { first, second }));

        // Act
        await orchestrator.CompensateAsync(sagaId);

        // Assert
        first.Compensations.Should().Be(1);
        second.Compensations.Should().Be(0);
    }

    [Fact]
    public async Task CompensateAsyncSkipsSagaThatIsAlreadyCompensated()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaInstance
        {
            Id = sagaId,
            SagaType = "TestSaga",
            State = SagaState.Compensated,
            CurrentStep = 2,
            SagaData = """{"value":1}""",
        };

        _repository.Setup(r => r.GetByIdAsync(sagaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);

        var step = new FakeSagaStep("First", 1, new List<string>());
        var orchestrator = CreateOrchestrator(new FakeSagaDefinition(
            "TestSaga", typeof(TestSagaData), new List<ISagaStep> { step }));

        // Act
        await orchestrator.CompensateAsync(sagaId);

        // Assert
        step.Compensations.Should().Be(0);
        saga.State.Should().Be(SagaState.Compensated);
    }

    [Fact]
    public async Task CompensateAsyncDoesNothingWhenSagaTypeHasNoDefinition()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaInstance
        {
            Id = sagaId,
            SagaType = "UnknownSaga",
            State = SagaState.Failed,
            CurrentStep = 1,
            SagaData = """{"value":1}""",
        };

        _repository.Setup(r => r.GetByIdAsync(sagaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saga);

        var orchestrator = CreateOrchestrator(definition: null);

        // Act
        await orchestrator.CompensateAsync(sagaId);

        // Assert
        saga.State.Should().Be(SagaState.Failed);
    }

    [Fact]
    public async Task CompensateAsyncDoesNothingWhenSagaIsMissing()
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SagaInstance?)null);

        var orchestrator = CreateOrchestrator(new FakeSagaDefinition(
            "TestSaga", typeof(TestSagaData), new List<ISagaStep> { new FakeSagaStep("First", 1, new List<string>()) }));

        // Act
        await orchestrator.CompensateAsync(Guid.NewGuid());

        // Assert
        _repository.Verify(r => r.SaveAsync(It.IsAny<SagaInstance>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private SagaOrchestrator CreateOrchestrator(ISagaDefinition? definition) =>
        new(_repository.Object, new FakeSagaRegistry(definition), _logger.Object);

    private sealed class TestSagaData
    {
        public int Value { get; set; }
    }

    private sealed class FakeSagaStep(string name, int order, List<string> callOrder) : ISagaStep
    {
        public string Name { get; } = name;
        public int Order { get; } = order;

        public int Compensations { get; private set; }

        public Task ExecuteAsync(object sagaData, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task CompensateAsync(object sagaData, CancellationToken cancellationToken = default)
        {
            Compensations++;
            callOrder.Add(Name);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSagaDefinition(
        string sagaType,
        Type sagaDataType,
        IReadOnlyList<ISagaStep> steps) : ISagaDefinition
    {
        public string SagaType { get; } = sagaType;
        public Type SagaDataType { get; } = sagaDataType;
        public IReadOnlyList<ISagaStep> Steps { get; } = steps;
    }

    private sealed class FakeSagaRegistry(ISagaDefinition? definition) : ISagaRegistry
    {
        public ISagaDefinition? GetDefinition(string sagaType) => definition;
    }
}

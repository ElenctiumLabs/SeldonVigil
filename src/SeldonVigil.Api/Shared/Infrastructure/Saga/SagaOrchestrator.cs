using System.Text.Json;
using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Infrastructure.Saga;

/// <summary>
/// Implementation of the saga orchestrator for distributed transaction coordination.
/// </summary>
public class SagaOrchestrator(
    ISagaRepository sagaRepository,
    ISagaRegistry sagaRegistry,
    ILogger<SagaOrchestrator> logger) : ISagaOrchestrator
{
    /// <inheritdoc/>
    public async Task ExecuteAsync<T>(
        string sagaType,
        string correlationId,
        IEnumerable<ISagaStep> steps,
        T sagaData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaData);

        var orderedSteps = steps.OrderBy(s => s.Order).ToList();
        var sagaInstance = new SagaInstance
        {
            Id = Guid.NewGuid(),
            SagaType = sagaType,
            CorrelationId = correlationId,
            State = SagaState.InProgress,
            CurrentStep = 0,
            TotalSteps = orderedSteps.Count,
            SagaData = JsonSerializer.Serialize(sagaData),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);

        try
        {
            for (int i = 0; i < orderedSteps.Count; i++)
            {
                var step = orderedSteps[i];
                sagaInstance.CurrentStep = i + 1;
                sagaInstance.UpdatedAt = DateTime.UtcNow;

                logger.LogInformation("Executing saga step {StepName} (step {CurrentStep}/{TotalSteps}) for saga {SagaId}",
                    step.Name, sagaInstance.CurrentStep, sagaInstance.TotalSteps, sagaInstance.Id);

                await step.ExecuteAsync(sagaData, cancellationToken).ConfigureAwait(false);

                // Update saga data after each step
                sagaInstance.SagaData = JsonSerializer.Serialize(sagaData);
                await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);
            }

            sagaInstance.State = SagaState.Completed;
            sagaInstance.CompletedAt = DateTime.UtcNow;
            sagaInstance.UpdatedAt = DateTime.UtcNow;

            await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Saga {SagaId} completed successfully", sagaInstance.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Saga {SagaId} failed at step {CurrentStep}", sagaInstance.Id, sagaInstance.CurrentStep);

            sagaInstance.State = SagaState.Failed;
            sagaInstance.ErrorMessage = ex.Message;

            // Persist the latest saga data so compensation sees the state the
            // failed step left behind, not just the last successful step.
            sagaInstance.SagaData = JsonSerializer.Serialize(sagaData);
            sagaInstance.UpdatedAt = DateTime.UtcNow;

            await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);

            // Roll back the steps that already ran.
            await CompensateAsync(sagaInstance.Id, cancellationToken).ConfigureAwait(false);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CompensateAsync(
        Guid sagaInstanceId,
        CancellationToken cancellationToken = default)
    {
        var sagaInstance = await sagaRepository.GetByIdAsync(sagaInstanceId, cancellationToken).ConfigureAwait(false);

        if (sagaInstance == null)
        {
            logger.LogWarning("Saga instance {SagaId} not found for compensation", sagaInstanceId);
            return;
        }

        if (sagaInstance.State == SagaState.Compensated || sagaInstance.State == SagaState.Pending)
        {
            logger.LogInformation("Saga {SagaId} is already in state {State}, skipping compensation", sagaInstanceId, sagaInstance.State);
            return;
        }

        var definition = sagaRegistry.GetDefinition(sagaInstance.SagaType);
        if (definition == null)
        {
            logger.LogWarning(
                "No saga definition registered for saga type {SagaType}; cannot compensate saga {SagaId}",
                sagaInstance.SagaType, sagaInstanceId);
            return;
        }

        var sagaData = JsonSerializer.Deserialize(sagaInstance.SagaData, definition.SagaDataType);
        if (sagaData == null)
        {
            logger.LogWarning(
                "Saga data for saga {SagaId} could not be deserialized as {SagaDataType}",
                sagaInstanceId, definition.SagaDataType.Name);
            return;
        }

        var completedStepCount = Math.Min(sagaInstance.CurrentStep, definition.Steps.Count);

        sagaInstance.State = SagaState.Compensating;
        sagaInstance.UpdatedAt = DateTime.UtcNow;
        await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Starting compensation for saga {SagaId}", sagaInstanceId);

        // Compensate the steps that already ran, in reverse order.
        for (var i = completedStepCount - 1; i >= 0; i--)
        {
            var step = definition.Steps[i];
            logger.LogInformation("Compensating step {StepName} for saga {SagaId}", step.Name, sagaInstanceId);
            await step.CompensateAsync(sagaData, cancellationToken).ConfigureAwait(false);
        }

        sagaInstance.State = SagaState.Compensated;
        sagaInstance.CompletedAt = DateTime.UtcNow;
        sagaInstance.UpdatedAt = DateTime.UtcNow;

        await sagaRepository.SaveAsync(sagaInstance, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Saga {SagaId} compensation completed", sagaInstanceId);
    }

    /// <inheritdoc/>
    public async Task<SagaInstance?> GetSagaInstanceAsync(
        Guid sagaInstanceId,
        CancellationToken cancellationToken = default)
    {
        return await sagaRepository.GetByIdAsync(sagaInstanceId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SagaInstance>> GetSagaInstancesByCorrelationIdAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        return await sagaRepository.GetByCorrelationIdAsync(correlationId, cancellationToken).ConfigureAwait(false);
    }
}

using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Infrastructure.Saga;

/// <summary>
/// Background service for monitoring and recovering failed sagas.
/// </summary>
public class SagaRecoveryService(
    IServiceProvider serviceProvider,
    ILogger<SagaRecoveryService> logger) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SagaRecoveryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverFailedSagasAsync(stoppingToken).ConfigureAwait(false);
                await CleanupCompletedSagasAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during saga recovery");
            }

            await Task.Delay(_checkInterval, stoppingToken).ConfigureAwait(false);
        }

        logger.LogInformation("SagaRecoveryService stopped");
    }

    private async Task RecoverFailedSagasAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();
        var sagaOrchestrator = scope.ServiceProvider.GetRequiredService<ISagaOrchestrator>();

        var failedSagas = await sagaRepository.GetByStateAsync(SagaState.Failed, cancellationToken).ConfigureAwait(false);

        foreach (var saga in failedSagas)
        {
            if (saga.RetryCount >= saga.MaxRetries)
            {
                logger.LogWarning("Saga {SagaId} has exceeded max retries ({RetryCount}/{MaxRetries}), skipping recovery",
                    saga.Id, saga.RetryCount, saga.MaxRetries);
                continue;
            }

            logger.LogInformation("Attempting to recover saga {SagaId} (attempt {RetryCount}/{MaxRetries})",
                saga.Id, saga.RetryCount + 1, saga.MaxRetries);

            try
            {
                saga.RetryCount++;
                saga.State = SagaState.InProgress;
                saga.UpdatedAt = DateTime.UtcNow;

                await sagaRepository.SaveAsync(saga, cancellationToken).ConfigureAwait(false);

                // Attempt to compensate the saga
                await sagaOrchestrator.CompensateAsync(saga.Id, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Successfully recovered saga {SagaId}", saga.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to recover saga {SagaId}", saga.Id);

                saga.State = SagaState.Failed;
                saga.ErrorMessage = ex.Message;
                saga.UpdatedAt = DateTime.UtcNow;

                await sagaRepository.SaveAsync(saga, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task CleanupCompletedSagasAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        var completedSagas = await sagaRepository.GetByStateAsync(SagaState.Completed, cancellationToken).ConfigureAwait(false);
        var compensatedSagas = await sagaRepository.GetByStateAsync(SagaState.Compensated, cancellationToken).ConfigureAwait(false);

        var toDelete = completedSagas
            .Concat(compensatedSagas)
            .Where(s => s.CompletedAt.HasValue && s.CompletedAt.Value < cutoffDate)
            .ToList();

        foreach (var saga in toDelete)
        {
            await sagaRepository.DeleteAsync(saga.Id, cancellationToken).ConfigureAwait(false);
        }

        if (toDelete.Count > 0)
        {
            logger.LogInformation("Deleted {Count} completed/compensated sagas older than {CutoffDate}",
                toDelete.Count, cutoffDate);
        }
    }
}

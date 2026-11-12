using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Shared.Infrastructure.Saga;

/// <summary>
/// PostgreSQL-backed saga repository using Entity Framework Core.
/// Replaces the in-memory implementation for production use.
/// </summary>
internal class PostgresSagaRepository(SeldonVigilDbContext dbContext, ILogger<PostgresSagaRepository> logger) : ISagaRepository
{
    /// <inheritdoc/>
    public async Task SaveAsync(SagaInstance sagaInstance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sagaInstance);

        var existing = await dbContext.SagaInstances
            .FirstOrDefaultAsync(s => s.Id == sagaInstance.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.SagaInstances.Add(sagaInstance);
            logger.LogDebug("Created saga instance {SagaId} with state {State}", sagaInstance.Id, sagaInstance.State);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(sagaInstance);
            logger.LogDebug("Updated saga instance {SagaId} to state {State}", sagaInstance.Id, sagaInstance.State);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<SagaInstance?> GetByIdAsync(Guid sagaInstanceId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SagaInstances
            .FirstOrDefaultAsync(s => s.Id == sagaInstanceId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SagaInstance>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SagaInstances
            .Where(s => s.CorrelationId == correlationId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid sagaInstanceId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SagaInstances
            .FirstOrDefaultAsync(s => s.Id == sagaInstanceId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        dbContext.SagaInstances.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogDebug("Deleted saga instance {SagaId}", sagaInstanceId);
    }

    /// <inheritdoc/>
    public async Task<List<SagaInstance>> GetByStateAsync(SagaState state, CancellationToken cancellationToken = default)
    {
        return await dbContext.SagaInstances
            .Where(s => s.State == state)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<List<SagaInstance>> GetPendingRetryAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SagaInstances
            .Where(s => s.State == SagaState.Failed && s.RetryCount < s.MaxRetries)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}


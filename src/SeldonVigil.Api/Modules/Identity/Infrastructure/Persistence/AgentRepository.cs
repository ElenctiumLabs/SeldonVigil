using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;

internal sealed partial class AgentRepository(
    SeldonVigilDbContext dbContext,
    ILogger<AgentRepository> logger)
    : IAgentRepository
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Agent added to database: {AgentId}")]
    private static partial void LogAgentAdded(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Agent updated in database: {AgentId}")]
    private static partial void LogAgentUpdated(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Agent soft-deleted: {AgentId}")]
    private static partial void LogAgentSoftDeleted(ILogger logger, string agentId);

    public ValueTask<Agent?> GetByIdAsync(AgentId id, CancellationToken ct = default)
    {
        return new ValueTask<Agent?>(
            dbContext.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, ct));
    }

    public ValueTask<Agent?> GetByExternalIdAsync(TenantId tenantId, string externalId, CancellationToken ct = default)
    {
        return new ValueTask<Agent?>(
            dbContext.Agents
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.TenantId == tenantId &&
                         a.ExternalId == externalId,
                    ct));
    }

    public async ValueTask<IReadOnlyList<Agent>> GetByTenantAsync(
        TenantId tenantId, AgentStatus? status = null, CancellationToken ct = default)
    {
        var query = dbContext.Agents
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        {
            var totalAll = await dbContext.Agents.CountAsync(ct);
            var withMine = await dbContext.Agents.Where(a => a.TenantId == tenantId).CountAsync(ct);
            var viaFilter = await query.CountAsync(ct);
                        {
                var dbConn = (Npgsql.NpgsqlConnection)dbContext.Database.GetDbConnection();
                if (dbConn.State != System.Data.ConnectionState.Open) await dbConn.OpenAsync(ct);
                using var raw = new Npgsql.NpgsqlCommand("SELECT current_database(), count(*) FROM agents", dbConn);
                using var r = await raw.ExecuteReaderAsync(ct);
                await r.ReadAsync(ct);
                Console.WriteLine($"###AGENTS-DIAG totalAll={totalAll} repoPredicate={withMine} withGlobalFilter={viaFilter} rawdb={r.GetString(0)} rawcount={r.GetInt64(1)}");
            }
        }
        var agents = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return agents.AsReadOnly();
    }

    public async ValueTask<IReadOnlyList<Agent>> GetByFrameworkAsync(
        TenantId tenantId, AgentFramework framework, CancellationToken ct = default)
    {
        var agents = await dbContext.Agents
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId &&
                        a.Framework == framework)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return agents.AsReadOnly();
    }

    public async ValueTask<bool> ExistsAsync(TenantId tenantId, string externalId, CancellationToken ct = default)
    {
        return await dbContext.Agents
            .AsNoTracking()
            .AnyAsync(a => a.TenantId == tenantId &&
                          a.ExternalId == externalId, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Agent agent, CancellationToken ct = default)
    {
        await dbContext.Agents.AddAsync(agent, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogAgentAdded(logger, agent.Id.ToString());
    }

    public async Task UpdateAsync(Agent agent, CancellationToken ct = default)
    {
        _ = dbContext.Agents.Update(agent);
        _ = await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogAgentUpdated(logger, agent.Id.ToString());
    }

    public async Task DeleteAsync(AgentId id, CancellationToken ct = default)
    {
        var agent = await dbContext.Agents.FindAsync([id], ct).ConfigureAwait(false);
        if (agent is not null)
        {
            agent.GetType().GetProperty("DeletedAt")?.SetValue(agent, DateTime.UtcNow);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogAgentSoftDeleted(logger, id.ToString());
        }
    }
}

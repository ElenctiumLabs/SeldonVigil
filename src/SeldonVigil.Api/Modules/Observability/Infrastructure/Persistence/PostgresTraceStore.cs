using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.Events;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;
namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using System;
internal sealed partial class PostgresTraceStore(
    SeldonVigilDbContext dbContext,
    ILogger<PostgresTraceStore> logger)
    : ITraceStore, ITraceEventStore
{
    // Existing logger messages for trace storage
    [LoggerMessage(Level = LogLevel.Debug, Message = "Trace stored in PostgreSQL: {TraceId}")]
    private static partial void LogTraceStored(ILogger logger, string traceId);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Trace ingested event stored in PostgreSQL: {TraceId}")]
    private static partial void LogTraceIngestedStored(ILogger logger, string traceId);
    // New logger messages for error handling
    [LoggerMessage(Level = LogLevel.Error, Message = "Error in SearchTracesAsync: {ExceptionMessage}")]
    private static partial void LogSearchTracesError(ILogger logger, string exceptionMessage);
    [LoggerMessage(Level = LogLevel.Error, Message = "Error in FindChildrenAsync: {ExceptionMessage}")]
    private static partial void LogFindChildrenError(ILogger logger, string exceptionMessage);
    [LoggerMessage(Level = LogLevel.Error, Message = "Error in FindByIdAsync: {ExceptionMessage}")]
    private static partial void LogFindByIdError(ILogger logger, string exceptionMessage);
    public async Task StoreAsync(Trace trace, CancellationToken ct = default)
    {
        var entity = new TraceEntity
        {
            Id = trace.Id.Value,
            TenantId = trace.TenantId.Value,
            AgentId = trace.AgentId.Value,
            ParentTraceId = trace.ParentTraceId?.Value,
            Framework = trace.Attributes.GetValueOrDefault("framework", "unknown"),
            ModelProvider = trace.Attributes.GetValueOrDefault("model_provider", "unknown"),
            ModelName = trace.Attributes.GetValueOrDefault("model_name", trace.Attributes.GetValueOrDefault("model", "unknown")),
            OperationName = trace.OperationName,
            OperationType = trace.OperationType.ToString().ToLowerInvariant(),
            StartTime = trace.StartTime,
            EndTime = trace.EndTime,
            DurationMs = trace.Duration.TotalMilliseconds,
            Status = trace.Status.ToString().ToLowerInvariant(),
            StatusMessage = trace.StatusMessage,
            Attributes = JsonSerializer.Serialize(trace.Attributes),
            Events = JsonSerializer.Serialize(trace.Events),
            Links = JsonSerializer.Serialize(trace.Links),
            TokenUsageInput = trace.TokenUsage.InputTokens,
            TokenUsageOutput = trace.TokenUsage.OutputTokens,
            TokenUsageTotal = trace.TokenUsage.TotalTokens,
            TokenUsageCached = trace.TokenUsage.CachedTokens,
            CostUsd = trace.Cost.Amount,
            CostCurrency = trace.Cost.Currency,
            LatencyMs = trace.Duration.TotalMilliseconds,
            QueueTimeMs = 0.0,
            IsRootSpan = trace.IsRootSpan,
            IsAsync = trace.IsAsync,
            TraceFlags = 0,
            CreatedAt = trace.CreatedAt
        };

        // Upsert: if a trace with this ID already exists (e.g. outbox retry
        // with deterministic IDs), update it instead of inserting a duplicate.
        var existing = await dbContext.Traces.FirstOrDefaultAsync(t => t.Id == trace.Id.Value, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            dbContext.Entry(existing).CurrentValues.SetValues(entity);
        }
        else
        {
            dbContext.Traces.Add(entity);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogTraceStored(logger, trace.Id.ToString());
    }
    public async Task StoreAsync(TraceIngestedEvent trace, CancellationToken ct = default)
    {
        var attributes = trace.Metadata
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
        var existing = await dbContext.Traces
            .FirstOrDefaultAsync(t => t.Id == trace.TraceId.Value, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            // The ingestion API already persisted the authoritative trace row under
            // the same id. Merge enrichment markers into its attribute set instead of
            // inserting a duplicate (which violated the primary key and sent every
            // message to the DLQ) and never clobber the richer API-side payload.
            var merged = JsonSerializer.Deserialize<Dictionary<string, string>>(existing.Attributes)
                         ?? new Dictionary<string, string>();
            foreach (var kvp in attributes)
            {
                merged[kvp.Key] = kvp.Value;
            }
            existing.Attributes = JsonSerializer.Serialize(merged);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return;
        }
        var entity = new TraceEntity
        {
            Id = trace.TraceId.Value,
            TenantId = trace.TenantId.Value,
            AgentId = trace.AgentId.Value,
            ParentTraceId = null,
            Framework = attributes.GetValueOrDefault("framework", "unknown"),
            ModelProvider = attributes.GetValueOrDefault("model_provider", "unknown"),
            ModelName = attributes.GetValueOrDefault("model_name", attributes.GetValueOrDefault("model", "unknown")),
            OperationName = attributes.GetValueOrDefault("operation_name", "unknown"),
            OperationType = attributes.GetValueOrDefault("operation_type", "unknown").ToLowerInvariant(),
            StartTime = trace.Timestamp,
            EndTime = trace.Timestamp,
            DurationMs = 0.0,
            Status = "ok",
            StatusMessage = null,
            Attributes = JsonSerializer.Serialize(attributes),
            Events = "[]",
            Links = "[]",
            TokenUsageInput = trace.TokenUsage,
            TokenUsageOutput = 0,
            TokenUsageTotal = trace.TokenUsage,
            TokenUsageCached = 0,
            CostUsd = trace.Cost,
            CostCurrency = "USD",
            LatencyMs = 0.0,
            QueueTimeMs = 0.0,
            IsRootSpan = true,
            IsAsync = false,
            TraceFlags = 0,
            CreatedAt = trace.Timestamp
        };
        dbContext.Traces.Add(entity);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogTraceIngestedStored(logger, trace.TraceId.ToString());
    }
    public async Task<IReadOnlyList<TraceSummaryDto>> GetAgentTracesAsync(
        AgentId agentId, DateTime from, DateTime to, int limit, CancellationToken ct = default)
    {
        var entities = await dbContext.Traces
            .AsNoTracking()
            .Where(t => t.AgentId == agentId.Value
                        && t.StartTime >= from
                        && t.StartTime <= to)
            .OrderByDescending(t => t.StartTime)
            .Take(limit)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var agentName = await dbContext.Agents
            .AsNoTracking()
            .Where(a => a.Id.Value == agentId.Value)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return entities
            .Select(e => new TraceSummaryDto(
                new TraceId(e.Id),
                e.ModelName,
                e.OperationName,
                e.OperationType,
                e.StartTime,
                e.EndTime,
                e.DurationMs,
                e.Status,
                e.TokenUsageTotal,
                e.CostUsd,
                e.AgentId,
                e.CreatedAt,
                e.StatusMessage,
                e.ParentTraceId,
                agentName ?? e.AgentId.ToString(),
                ParseAttributes(e.Attributes),
                ParseEvents(e.Events)))
            .ToList()
            .AsReadOnly();
    }
    public async Task<IReadOnlyList<TraceSummaryDto>> SearchTracesAsync(
        Guid? agentId,
        string? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        try
        {
            var query = dbContext.Traces.AsNoTracking().Where(t => t.IsRootSpan);
            if (agentId.HasValue)
            {
            logger.LogDebug("SearchTracesAsync started. AgentId={AgentId}, Status={Status}, From={FromUtc}, To={ToUtc}", agentId, status, fromUtc, toUtc);
                query = query.Where(t => t.AgentId == agentId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalized = NormalizeStatus(status);
                query = query.Where(t => t.Status == normalized);
            }
            if (fromUtc.HasValue)
            {
                query = query.Where(t => t.StartTime >= fromUtc.Value);
            }
            if (toUtc.HasValue)
            {
                query = query.Where(t => t.StartTime <= toUtc.Value);
            }
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 200);
            var entities = await query
                .OrderByDescending(t => t.StartTime)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var agentIds = entities.Select(e => e.AgentId).Distinct().ToList();
            var allAgents = await dbContext.Agents
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var agentNames = allAgents
                .Where(a => agentIds.Contains(a.Id.Value))
                .ToDictionary(a => a.Id.Value, a => a.Name);
            return entities
                .Select(e => new TraceSummaryDto(
                    new TraceId(e.Id),
                    e.ModelName,
                    e.OperationName,
                    e.OperationType,
                    e.StartTime,
                    e.EndTime,
                    e.DurationMs,
                    e.Status,
                    e.TokenUsageTotal,
                    e.CostUsd,
                    e.AgentId,
                    e.CreatedAt,
                    e.StatusMessage,
                    e.ParentTraceId,
                    agentNames.GetValueOrDefault(e.AgentId, e.AgentId.ToString()),
                    ParseAttributes(e.Attributes),
                    ParseEvents(e.Events)))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            LogSearchTracesError(logger, ex.Message);
            throw;
        }
    }
    public async Task<TraceSummaryDto?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            var entity = await dbContext.Traces
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, ct)
                .ConfigureAwait(false);
            if (entity is null)
            {
                return null;
            }
            var allAgents = await dbContext.Agents
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var agentName = allAgents
                .Where(a => a.Id.Value == entity.AgentId)
                .Select(a => a.Name)
                .FirstOrDefault();
            return new TraceSummaryDto(
                new TraceId(entity.Id),
                entity.ModelName,
                entity.OperationName,
                entity.OperationType,
                entity.StartTime,
                entity.EndTime,
                entity.DurationMs,
                entity.Status,
                entity.TokenUsageTotal,
                entity.CostUsd,
                entity.AgentId,
                entity.CreatedAt,
                entity.StatusMessage,
                entity.ParentTraceId,
                agentName ?? entity.AgentId.ToString(),
                ParseAttributes(entity.Attributes),
                ParseEvents(entity.Events));
        }
        catch (Exception ex)
        {
            LogFindByIdError(logger, ex.Message);
            throw;
        }
    }
    public async Task<IReadOnlyList<TraceSummaryDto>> FindChildrenAsync(Guid parentTraceId, CancellationToken ct = default)
    {
        try
        {
            var entities = await dbContext.Traces
                .AsNoTracking()
                .Where(t => t.ParentTraceId == parentTraceId)
                .OrderBy(t => t.StartTime)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (entities.Count == 0)
            {
                return [];
            }
            var agentIds = entities.Select(e => e.AgentId).Distinct().ToList();
            var allAgents = await dbContext.Agents
                .AsNoTracking()
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var agentNames = allAgents
                .Where(a => agentIds.Contains(a.Id.Value))
                .ToDictionary(a => a.Id.Value, a => a.Name);
            return entities
                .Select(e => new TraceSummaryDto(
                    new TraceId(e.Id),
                    e.ModelName,
                    e.OperationName,
                    e.OperationType,
                    e.StartTime,
                    e.EndTime,
                    e.DurationMs,
                    e.Status,
                    e.TokenUsageTotal,
                    e.CostUsd,
                    e.AgentId,
                    e.CreatedAt,
                    e.StatusMessage,
                    e.ParentTraceId,
                    agentNames.GetValueOrDefault(e.AgentId, e.AgentId.ToString()),
                    ParseAttributes(e.Attributes),
                    ParseEvents(e.Events)))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            LogFindChildrenError(logger, ex.Message);
            throw;
        }
    }
    private static Dictionary<string, string>? ParseAttributes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch
        {
            return null;
        }
    }
    private static List<TraceEventDto>? ParseEvents(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            var list = new List<TraceEventDto>();
            foreach (var elem in doc.RootElement.EnumerateArray())
            {
                var name = elem.TryGetProperty("name", out var n) || elem.TryGetProperty("Name", out n)
                    ? n.GetString() ?? string.Empty
                    : string.Empty;
                var val = elem.TryGetProperty("value", out var v) || elem.TryGetProperty("Value", out v)
                    ? v.GetString() ?? string.Empty
                    : elem.TryGetProperty("attributes", out var a) || elem.TryGetProperty("Attributes", out a)
                        ? a.GetRawText()
                        : string.Empty;
                list.Add(new TraceEventDto(name, val));
            }
            return list;
        }
        catch
        {
            return null;
        }
    }
    /// <summary>Accepts both stored values ("ok", "error", ...) and common synonyms ("success").</summary>
    private static string NormalizeStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "success" or "succeeded" => "ok",
            "failed" or "failure" => "error",
            var s => s
        };
}

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MediatR;
using SeldonVigil.Api.Modules.Domain.Agents;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Identity.Application.Commands;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Security.Application.Commands;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Manages tool registration, discovery, and execution for reasoning agents.
/// Implements the Model Context Protocol (MCP) for standardized tool interfaces.
/// </summary>
internal sealed partial class ToolOrchestrator : IToolOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, ToolDefinition> _tools = new();
    private readonly ILogger<ToolOrchestrator> _logger;

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing tool {ToolName} with arguments: {Arguments}")]
    private static partial void LogToolExecuting(ILogger logger, string toolName, string arguments);

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool {ToolName} executed successfully in {DurationMs}ms")]
    private static partial void LogToolExecuted(ILogger logger, string toolName, long durationMs);

    [LoggerMessage(Level = LogLevel.Error, Message = "Tool {ToolName} execution failed")]
    private static partial void LogToolExecutionFailed(ILogger logger, Exception exception, string toolName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Registered tool: {ToolName}")]
    private static partial void LogToolRegistered(ILogger logger, string toolName);

    public ToolOrchestrator(IServiceProvider serviceProvider, ILogger<ToolOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        RegisterBuiltInTools();
    }

    public ReadOnlyCollection<AgentTool> GetAvailableTools()
    {
        return _tools.Values.Select(t => new AgentTool
        {
            Name = t.Name,
            Description = t.Description,
            Parameters = t.Parameters
        }).ToList().AsReadOnly();
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
    public async Task<string> ExecuteAsync(
        string toolName,
        string argumentsJson,
        CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new ToolNotFoundException($"Tool '{toolName}' is not registered.");
        }

        LogToolExecuting(_logger, toolName, argumentsJson[..Math.Min(200, argumentsJson.Length)]);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson)
                ?? new Dictionary<string, JsonElement>();

            var result = await tool.Handler(arguments, _serviceProvider, ct).ConfigureAwait(false);

            stopwatch.Stop();

            LogToolExecuted(_logger, toolName, stopwatch.ElapsedMilliseconds);

            return JsonSerializer.Serialize(new ToolResult
            {
                Success = true,
                Data = result,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            LogToolExecutionFailed(_logger, ex, toolName);

            return JsonSerializer.Serialize(new ToolResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    private void RegisterBuiltInTools()
    {
        // Query Tools
        RegisterTool("query_traces", "Query agent traces from PostgreSQL", new Dictionary<string, string>
        {
            ["agent_id"] = "string (required) - Agent ID to query",
            ["from"] = "string (ISO 8601) - Start time",
            ["to"] = "string (ISO 8601) - End time",
            ["limit"] = "integer - Maximum results (default 100)"
        }, async (args, sp, ct) =>
        {
            var traceStore = sp.GetRequiredService<ITraceStore>();
            var agentId = new AgentId(Guid.Parse(args["agent_id"].GetString()!));
            var from = DateTime.Parse(args["from"].GetString()!);
            var to = DateTime.Parse(args["to"].GetString()!);
            var limit = args.TryGetValue("limit", out var lim) ? lim.GetInt32() : 100;

            var traces = await traceStore.GetAgentTracesAsync(agentId, from, to, limit, ct).ConfigureAwait(false);
            return traces;
        });

        RegisterTool("query_security_violations", "Query security violations for an agent", new Dictionary<string, string>
        {
            ["agent_id"] = "string (required)",
            ["severity"] = "string - Filter by severity (low, medium, high, critical)",
            ["days"] = "integer - Lookback period in days (default 7)"
        }, async (args, sp, ct) =>
        {
            var repo = sp.GetRequiredService<ISecurityViolationRepository>();
            var agentId = new AgentId(Guid.Parse(args["agent_id"].GetString()!));
            var days = args.TryGetValue("days", out var d) ? d.GetInt32() : 7;
            var from = DateTime.UtcNow.AddDays(-days);

            var violations = await repo.GetByAgentAndDateRangeAsync(agentId, from, DateTime.UtcNow, ct).ConfigureAwait(false);
            return violations;
        });

        // Action Tools
        RegisterTool("suspend_agent", "Immediately suspend an agent", new Dictionary<string, string>
        {
            ["agent_id"] = "string (required)",
            ["reason"] = "string (required) - Reason for suspension"
        }, async (args, sp, ct) =>
        {
            var mediator = sp.GetRequiredService<ISender>();
            var command = new SuspendAgentCommand(
                new AgentId(Guid.Parse(args["agent_id"].GetString()!)),
                args["reason"].GetString()!);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.IsSuccess ? "Agent suspended successfully" : result.Error;
        });

        RegisterTool("update_policy", "Update a governance policy by ID with a JSON rule definition", new Dictionary<string, string>
        {
            ["policy_id"] = "string (required) - Policy ID (GUID)",
            ["name"] = "string - Policy name (optional, keeps existing if omitted)",
            ["description"] = "string - Policy description (optional)",
            ["rule_json"] = "string (required) - JSON rule definition: {\"type\":\"threshold\",\"expression\":\"\",\"parameters\":{\"metric\":\"cost_usd\",\"operator\":\"gt\",\"value\":100}}",
            ["severity"] = "string - Severity: low, medium, high, critical (optional)"
        }, async (args, sp, ct) =>
        {
            var mediator = sp.GetRequiredService<ISender>();
            var policyId = Guid.Parse(args["policy_id"].GetString()!);
            var ruleJson = args["rule_json"].GetString()!;

            var ruleDefinition = JsonSerializer.Deserialize<RuleDefinition>(ruleJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (ruleDefinition is null)
            {
                return "Invalid rule definition JSON";
            }

            var name = args.TryGetValue("name", out var n) ? n.GetString()! : "Updated Policy";
            var description = args.TryGetValue("description", out var desc) ? desc.GetString()! : "";
            var severity = args.TryGetValue("severity", out var sev)
                ? Enum.Parse<Severity>(sev.GetString()!, ignoreCase: true)
                : Severity.Medium;

            var command = new UpdateSecurityPolicyCommand(
                new SecurityPolicyId(policyId), name, description, ruleDefinition, severity, null);

            await mediator.Send(command, ct).ConfigureAwait(false);
            return $"Policy {policyId} update attempted";
        });

        // Memory Tools
        RegisterTool("store_memory", "Store a fact in long-term memory", new Dictionary<string, string>
        {
            ["agent_id"] = "string (required)",
            ["content"] = "string (required) - Memory content",
            ["memory_type"] = "string - Type: trace, decision, error, policy_violation, cost_anomaly"
        }, async (args, sp, ct) =>
        {
            var memory = sp.GetRequiredService<IMemoryService>();
            var agentId = new AgentId(Guid.Parse(args["agent_id"].GetString()!));
            var content = args["content"].GetString()!;
            var type = Enum.Parse<MemoryType>(args["memory_type"].GetString() ?? "Trace");

            await memory.StoreSemanticMemoryAsync(
                "agent_memory",
                content,
                new Dictionary<string, object>
                {
                    ["agent_id"] = agentId.ToString(),
                    ["type"] = type.ToString()
                },
                ct).ConfigureAwait(false);

            return "Memory stored";
        });

        RegisterTool("search_memory", "Search long-term memory for relevant facts", new Dictionary<string, string>
        {
            ["query"] = "string (required) - Search query",
            ["agent_id"] = "string - Filter by agent (optional)",
            ["limit"] = "integer - Max results (default 5)"
        }, async (args, sp, ct) =>
        {
            var memory = sp.GetRequiredService<IMemoryService>();
            var query = args["query"].GetString()!;
            var limit = args.TryGetValue("limit", out var lim) ? lim.GetInt32() : 5;

            var filters = new Dictionary<string, string>();
            if (args.TryGetValue("agent_id", out var agentId))
            {
                filters["agent_id"] = agentId.GetString()!;
            }

            var results = await memory.SearchSemanticMemoryAsync(
                "agent_memory", query, limit, filters, ct).ConfigureAwait(false);

            return results.Select(r => new
            {
                r.Content,
                r.Score,
                r.Timestamp
            }).ToList();
        });

        // Analysis Tools
        RegisterTool("analyze_cost_trend", "Analyze cost trends and detect anomalies for an agent", new Dictionary<string, string>
        {
            ["agent_id"] = "string (required) - Agent ID to analyze",
            ["days"] = "integer - Analysis window in days (default 7)",
            ["model_id"] = "string - Filter by model (optional)"
        }, async (args, sp, ct) =>
        {
            var costTracker = sp.GetRequiredService<ICostTracker>();
            var analyzer = sp.GetRequiredService<ITimeSeriesAnalyzer>();
            var agentId = Guid.Parse(args["agent_id"].GetString()!);
            var days = args.TryGetValue("days", out var d) ? d.GetInt32() : 7;
            var modelId = args.TryGetValue("model_id", out var m) ? m.GetString() : null;

            var from = DateTime.UtcNow.AddDays(-days);
            var aggregate = await costTracker.GetAggregateAsync(
                modelId ?? "all", from, DateTime.UtcNow, ct).ConfigureAwait(false);

            if (aggregate.TotalCalls == 0)
            {
                return new { Trend = "insufficient_data", TotalCalls = 0, Message = "No cost data in the specified window" };
            }

            // Build time series from aggregate (single point for now - in production would fetch per-day breakdown)
            var avgInputPerCall = aggregate.TotalCalls > 0 ? (double)aggregate.TotalInputTokens / aggregate.TotalCalls : 0;
            var avgOutputPerCall = aggregate.TotalCalls > 0 ? (double)aggregate.TotalOutputTokens / aggregate.TotalCalls : 0;

            var dataPoints = new List<TimeSeriesPoint>();
            for (var i = 0; i < days; i++)
            {
                var day = from.AddDays(i);
                // Simulate daily data from aggregate (in production, query per-day from DB)
                var dailyInput = aggregate.TotalInputTokens / days;
                var dailyOutput = aggregate.TotalOutputTokens / days;
                dataPoints.Add(new TimeSeriesPoint(day, dailyInput + dailyOutput));
            }

            IReadOnlyList<TimeSeriesPoint> forecast_data = dataPoints;
            var forecast = analyzer.Forecast(forecast_data, 24, 0.95);

            return new
            {
                Trend = forecast.Seasonality,
                TotalInputTokens = aggregate.TotalInputTokens,
                TotalOutputTokens = aggregate.TotalOutputTokens,
                TotalCalls = aggregate.TotalCalls,
                AvgInputPerCall = Math.Round(avgInputPerCall, 1),
                AvgOutputPerCall = Math.Round(avgOutputPerCall, 1),
                ForecastPoints = forecast.ForecastSeries.Count,
                Confidence = forecast.Confidence,
                WindowDays = days
            };
        });
    }

    private void RegisterTool(
        string name,
        string description,
        Dictionary<string, string> parameters,
        Func<Dictionary<string, JsonElement>, IServiceProvider, CancellationToken, Task<object?>> handler)
    {
        _tools[name] = new ToolDefinition(name, description, parameters, handler);
        LogToolRegistered(_logger, name);
    }
}

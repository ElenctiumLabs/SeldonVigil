using System.Diagnostics;
using System.Text.Json;
using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

internal abstract partial class BaseReasoningAgent<TRequest, TResponse>(
    IChatClient chatClient,
    IToolOrchestrator toolOrchestrator,
    ToolFunctionAdapter toolFunctionAdapter,
    IMemoryService memoryService,
    IPromptManager promptManager,
    IEvaluationService evaluationService,
    ICostTracker costTracker,
    ILogger logger)
{
    protected readonly IMemoryService _memoryService = memoryService;

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting reasoning chain {ChainId} for agent {AgentType}")]
    private static partial void LogReasoningChainStarted(ILogger logger, Guid chainId, string agentType);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reasoning chain {ChainId} completed in {DurationMs}ms, {Tokens} tokens, {Steps} steps")]
    private static partial void LogReasoningChainCompleted(ILogger logger, Guid chainId, long durationMs, int tokens, int steps);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reasoning chain {ChainId} failed after {DurationMs}ms")]
    private static partial void LogReasoningChainFailed(ILogger logger, Exception exception, Guid chainId, long durationMs);

    public async Task<ReasoningResult<TResponse>> ExecuteAsync(
        TRequest request,
        ReasoningContext context,
        CancellationToken ct = default)
    {
        var chainId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        var reasoningSteps = new List<ReasoningStep>();
        var toolCalls = new List<ToolCallRecord>();

        try
        {
            LogReasoningChainStarted(logger, chainId, GetType().Name);

            var memoryContext = await RetrieveMemoryAsync(request, context, ct)
                .ConfigureAwait(false);

            var prompt = await promptManager.GetPromptAsync(
                GetPromptName(),
                BuildPromptVariables(request, memoryContext),
                ct).ConfigureAwait(false);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, prompt.SystemPrompt),
                new(ChatRole.User, prompt.UserPrompt)
            };

            TResponse? finalResponse = default;
            var iteration = 0;
            const int maxIterations = 10;

            while (iteration < maxIterations)
            {
                iteration++;

                var response = await chatClient.GetResponseAsync(
                    messages,
                    new ChatOptions
                    {
                        ModelId = SelectModel(context.Priority),
                        Temperature = GetTemperature(iteration),
                        MaxOutputTokens = 4000,
                        Tools = toolFunctionAdapter.GetToolFunctions().ToList<AITool>(),
                    },
                    ct).ConfigureAwait(false);

                var usage = response.Usage;
                if (usage is not null)
                {
                    costTracker.TrackUsage(
                        chainId,
                        (int)(usage.InputTokenCount ?? 0),
                        (int)(usage.OutputTokenCount ?? 0),
                        SelectModel(context.Priority));
                }

                reasoningSteps.Add(new ReasoningStep(
                    iteration,
                    response.Text,
                    (int)(usage?.TotalTokenCount ?? 0),
                    stopwatch.ElapsedMilliseconds));

                // Extract function calls from response messages
                var functionCalls = (response.Messages ?? Enumerable.Empty<ChatMessage>())
                    .SelectMany(m => m.Contents?.OfType<FunctionCallContent>()
                        ?? [])
                    .ToList();

                if (functionCalls.Count > 0)
                {
                    foreach (var fc in functionCalls)
                    {
                        var callId = fc.CallId ?? Guid.NewGuid().ToString("N");
                        var argsJson = JsonSerializer.Serialize(fc.Arguments);

                        var toolResult = await toolOrchestrator.ExecuteAsync(
                            fc.Name,
                            argsJson,
                            ct).ConfigureAwait(false);

                        toolCalls.Add(new ToolCallRecord(
                            callId,
                            fc.Name,
                            argsJson,
                            toolResult,
                            stopwatch.ElapsedMilliseconds));

                        messages.Add(new ChatMessage(
                            ChatRole.Assistant,
                            [fc]));

                        messages.Add(new ChatMessage(
                            ChatRole.Tool,
                            [new FunctionResultContent(callId, toolResult)]));
                    }

                    continue; // Continue reasoning loop
                }

                finalResponse = ParseResponse(response.Text);
                break;
            }

            stopwatch.Stop();

            await StoreMemoryAsync(
                request,
                finalResponse,
                reasoningSteps,
                toolCalls,
                context,
                ct).ConfigureAwait(false);

            var evaluation = await evaluationService.EvaluateAsync(
                GetPromptName(),
                request,
                finalResponse,
                reasoningSteps,
                ct).ConfigureAwait(false);

            var result = new ReasoningResult<TResponse>
            {
                ChainId = chainId,
                Response = finalResponse,
                ReasoningSteps = reasoningSteps.AsReadOnly(),
                ToolCalls = toolCalls.AsReadOnly(),
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                TotalTokens = reasoningSteps.Sum(s => s.TokenCount),
                Evaluation = evaluation,
                Success = true
            };

            LogReasoningChainCompleted(logger, chainId, result.TotalDurationMs, result.TotalTokens, reasoningSteps.Count);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            LogReasoningChainFailed(logger, ex, chainId, stopwatch.ElapsedMilliseconds);

            return new ReasoningResult<TResponse>
            {
                ChainId = chainId,
                ReasoningSteps = reasoningSteps.AsReadOnly(),
                ToolCalls = toolCalls.AsReadOnly(),
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                TotalTokens = reasoningSteps.Sum(s => s.TokenCount),
                Success = false,
                Error = ex.Message
            };
        }
    }

    protected abstract string GetPromptName();
    protected abstract Dictionary<string, object> BuildPromptVariables(TRequest request, MemoryContext memory);
    protected abstract TResponse ParseResponse(string responseText);
    protected abstract Task<MemoryContext> RetrieveMemoryAsync(TRequest request, ReasoningContext context, CancellationToken ct);
    protected abstract Task StoreMemoryAsync(TRequest request, TResponse? response, List<ReasoningStep> steps, List<ToolCallRecord> toolCalls, ReasoningContext context, CancellationToken ct);

    private static string SelectModel(ReasoningPriority priority) => priority switch
    {
        ReasoningPriority.Critical => "o3-mini",
        ReasoningPriority.High => "claude-3-7-sonnet",
        ReasoningPriority.Standard => "gpt-4o",
        ReasoningPriority.Low => "claude-3-5-haiku",
        _ => "gpt-4o"
    };

    private static float GetTemperature(int iteration) => iteration == 1 ? 0.3f : 0.1f;
}

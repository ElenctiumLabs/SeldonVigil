using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Governance.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.Repositories;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Workers.Workers;

/// <summary>
/// Drives certification pipelines through their automated stages. Prior to this
/// worker, <c>CertificationPipeline.ProgressStage</c> existed and was unit-tested
/// but had no caller, so pipelines were created and then sat in <c>Pending</c>
/// forever — only reachable through a manual approve. This worker executes the
/// three automated stages (static_analysis, security_scan, policy_validation)
/// against real system signals and advances state accordingly.
///
/// The <c>manual_approval</c> stage is intentionally NOT driven here: it is a
/// human gate resolved through the existing approve endpoint.
/// </summary>
internal sealed partial class CertificationPipelineWorker(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    IDateTimeProvider dateTimeProvider,
    ILogger<CertificationPipelineWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SecurityScanWindow = TimeSpan.FromDays(30);

    [LoggerMessage(Level = LogLevel.Information, Message = "Certification Pipeline Worker started")]
    private static partial void LogWorkerStarted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Certification scan cycle complete: {Processed} pipeline(s) advanced")]
    private static partial void LogCycleComplete(ILogger logger, int processed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error in certification pipeline loop")]
    private static partial void LogLoopError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stage {Stage} failed for pipeline {PipelineId}: {Reason}")]
    private static partial void LogStageFailed(ILogger logger, string stage, Guid pipelineId, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stage {Stage} passed for pipeline {PipelineId}")]
    private static partial void LogStagePassed(ILogger logger, string stage, Guid pipelineId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Pipeline {PipelineId} reached status {Status}")]
    private static partial void LogPipelineStatus(ILogger logger, Guid pipelineId, string status);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStarted(logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessActivePipelinesAsync(stoppingToken).ConfigureAwait(false);
                LogCycleComplete(logger, processed);
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogLoopError(logger, ex);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<int> ProcessActivePipelinesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var pipelineRepository = scope.ServiceProvider.GetRequiredService<CertificationPipelineRepository>();

        var active = await pipelineRepository.GetActiveAsync(ct).ConfigureAwait(false);
        var processed = 0;

        foreach (var pipeline in active)
        {
            ct.ThrowIfCancellationRequested();

            // CertificationPipeline itself has no tenant query filter, but the
            // repositories its stages read from (Agent, SecurityViolation) do.
            // Set the ambient tenant for the duration of this pipeline's work.
            using var _ = TenantScope.Begin(pipeline.TenantId.Value);

            var stageName = pipeline.CurrentStage;
            if (string.IsNullOrEmpty(stageName))
            {
                continue;
            }

            // manual_approval is a human gate — never auto-advance it here.
            if (string.Equals(stageName, "manual_approval", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var evaluation = await EvaluateStageAsync(stageName, pipeline.AgentId, scope.ServiceProvider, ct)
                .ConfigureAwait(false);

            var result = pipeline.ProgressStage(stageName, evaluation.Success, evaluation.Output, dateTimeProvider);
            if (result.IsFailure)
            {
                LogStageFailed(logger, stageName, pipeline.Id.Value, result.Error?.Message ?? "unknown");
                continue;
            }

            await pipelineRepository.SaveAsync(pipeline, ct).ConfigureAwait(false);

            if (evaluation.Success)
            {
                LogStagePassed(logger, stageName, pipeline.Id.Value);
            }
            else
            {
                LogStageFailed(logger, stageName, pipeline.Id.Value, evaluation.Output);
            }

            await PublishAggregateEventsAsync(pipeline, ct).ConfigureAwait(false);
            LogPipelineStatus(logger, pipeline.Id.Value, pipeline.Status.ToString());
            processed++;
        }

        return processed;
    }

    private async Task<StageEvaluation> EvaluateStageAsync(
        string stageName,
        SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.AgentId agentId,
        IServiceProvider services,
        CancellationToken ct)
    {
        var agentRepository = services.GetRequiredService<IAgentRepository>();
        var violationRepository = services.GetRequiredService<ISecurityViolationRepository>();
        var agent = await agentRepository.GetByIdAsync(agentId, ct).ConfigureAwait(false);

        switch (stageName)
        {
            case "static_analysis":
                return EvaluateStaticAnalysis(agent);

            case "security_scan":
            {
                if (agent is null)
                {
                    return new StageEvaluation(false, "Agent not found; cannot run security scan.");
                }

                var to = dateTimeProvider.UtcNow;
                var from = to - SecurityScanWindow;
                var violations = await violationRepository
                    .GetByAgentAndDateRangeAsync(agentId, from, to, ct).ConfigureAwait(false);

                var blocking = violations
                    .Where(v => v.Status is ViolationStatus.Open or ViolationStatus.Investigating)
                    .Where(v => v.Severity is Severity.High or Severity.Critical)
                    .ToList();

                return blocking.Count == 0
                    ? new StageEvaluation(true, $"No open High/Critical violations in the last {SecurityScanWindow.Days} days.")
                    : new StageEvaluation(false, $"{blocking.Count} open High/Critical violation(s) in the last {SecurityScanWindow.Days} days.");
            }

            case "policy_validation":
                return EvaluatePolicyValidation(agent);

            default:
                return new StageEvaluation(false, $"Unknown stage '{stageName}'; no automated evaluator registered.");
        }
    }

    internal static StageEvaluation EvaluateStaticAnalysis(
        SeldonVigil.Api.Modules.Identity.Domain.Entities.Agent? agent)
    {
        if (agent is null)
        {
            return new StageEvaluation(false, "Agent not found; cannot verify source provenance.");
        }

        // NOTE: Agent.SourceCodeHash / SourceVersion / PromptTemplateHash are get-only
        // and never assigned anywhere in the codebase (always null/0), and
        // CapabilityManifestHash is auto-computed at registration — so any check keyed
        // on those fields can never fail for a registered agent. The capability manifest
        // is the only field that genuinely varies per agent, so the gate inspects it for
        // real completeness/coherence signals instead.
        var manifest = agent.CapabilityManifest;

        if (manifest is null)
        {
            return new StageEvaluation(false, "No capability manifest recorded.");
        }

        var problems = new List<string>();

        if (manifest.Permissions.Count == 0 && manifest.Tools.Count == 0 && manifest.DataAccess.Count == 0)
        {
            problems.Add("manifest declares no permissions, tools, or data-access rules");
        }

        if (manifest.RateLimits is null
            || manifest.RateLimits.RequestsPerMinute <= 0
            || manifest.RateLimits.RequestsPerHour <= 0)
        {
            problems.Add("rate limits are missing or non-positive");
        }

        problems.AddRange(manifest.Tools
            .Where(tool => string.IsNullOrWhiteSpace(tool.Name))
            .Select(_ => "a declared tool has no name"));

        problems.AddRange(manifest.DataAccess
            .Where(rule => string.IsNullOrWhiteSpace(rule.Resource))
            .Select(_ => "a data-access rule has no resource"));

        return problems.Count == 0
            ? new StageEvaluation(true,
                $"Capability manifest coherent ({manifest.Tools.Count} tool(s), {manifest.DataAccess.Count} data-access rule(s), {manifest.Permissions.Count} permission(s)).")
            : new StageEvaluation(false, "Manifest issues: " + string.Join("; ", problems) + ".");
    }

    internal static StageEvaluation EvaluatePolicyValidation(
        SeldonVigil.Api.Modules.Identity.Domain.Entities.Agent? agent)
    {
        if (agent is null)
        {
            return new StageEvaluation(false, "Agent not found; cannot validate policy compliance.");
        }

        var compliant = agent.Status is AgentStatus.Active or AgentStatus.PendingCertification;

        return compliant
            ? new StageEvaluation(true, $"Agent lifecycle status '{agent.Status}' is policy-compliant.")
            : new StageEvaluation(false, $"Agent lifecycle status '{agent.Status}' is not policy-compliant for certification.");
    }

    /// <summary>
    /// Publishes the domain events the aggregate recorded during <c>ProgressStage</c>
    /// and clears them, mirroring how SecurityAnalyzerWorker emits its events
    /// explicitly (this codebase does not auto-dispatch on SaveChanges).
    /// </summary>
    private async Task PublishAggregateEventsAsync(CertificationPipeline pipeline, CancellationToken ct)
    {
        foreach (var domainEvent in pipeline.DomainEvents.ToList())
        {
            await eventBus.PublishAsync(domainEvent, ct).ConfigureAwait(false);
        }
        pipeline.ClearDomainEvents();
    }

    internal readonly record struct StageEvaluation(bool Success, string Output);
}

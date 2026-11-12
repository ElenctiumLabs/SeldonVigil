using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MediatR;
using SeldonVigil.Api.Modules.AIReasoning.Application.Commands;
using SeldonVigil.Api.Modules.AIReasoning.Domain.Entities;
using SeldonVigil.Api.Modules.AIReasoning.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.AIReasoning.Api.Endpoints;

internal sealed record RootCauseAnalysisDto(
    string Id,
    string TenantId,
    string AgentId,
    string? TraceId,
    string AnomalyType,
    DateTime AnomalyStartTime,
    DateTime AnomalyEndTime,
    string ProbableRootCause,
    double Confidence,
    string ConfidenceLabel,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> ContributingFactors,
    IReadOnlyList<RecommendedActionDto> RecommendedActions,
    IReadOnlyList<string> RelatedTraceIds,
    string Status,
    DateTime CreatedAt);

internal sealed record RecommendedActionDto(
    string Action,
    string Priority,
    string Owner);

internal sealed record TriggerRcaRequest(
    string AgentId,
    string AnomalyType,
    DateTime AnomalyStartTime,
    DateTime AnomalyEndTime,
    string? TraceId);

internal static class AIReasoningEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapAIReasoningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai-reasoning")
            .WithTags("AI Reasoning")

            .RequireAuthorization();

        group.MapPost("/root-cause-analysis", async (
            TriggerRcaRequest request,
            ISender mediator,
            IRequestContext requestContext,
            RootCauseAnalysisRepository repository,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.AgentId))
            {
                return Results.BadRequest(new { error = "AgentId is required" });
            }

            if (string.IsNullOrWhiteSpace(request.AnomalyType))
            {
                return Results.BadRequest(new { error = "AnomalyType is required" });
            }

            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Results.BadRequest(new { error = "Tenant context is required" });
            }

            var agentId = new AgentId(Guid.Parse(request.AgentId));
            TraceId? traceId = request.TraceId is not null ? new TraceId(Guid.Parse(request.TraceId)) : null;

            var entity = RootCauseAnalysis.Create(
                tenantId,
                request.AgentId,
                request.AnomalyType,
                request.AnomalyStartTime,
                request.AnomalyEndTime,
                request.TraceId);

            var command = new GenerateRootCauseAnalysisCommand(
                new TenantId(Guid.Parse(tenantId)),
                request.AnomalyType,
                agentId,
                traceId,
                request.AnomalyStartTime,
                request.AnomalyEndTime);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);

            return await result.Match<Task<IResult>>(
                async rca =>
                {
                    entity.Complete(
                        rca.ProbableRootCause,
                        rca.Confidence,
                        rca.ConfidenceLabel,
                        JsonSerializer.Serialize(rca.Evidence),
                        JsonSerializer.Serialize(rca.ContributingFactors),
                        JsonSerializer.Serialize(rca.RecommendedActions),
                        JsonSerializer.Serialize(rca.RelatedTraceIds));
                    await repository.AddAsync(entity, ct).ConfigureAwait(false);
                    return Results.Ok(MapToDto(entity));
                },
                _ => Task.FromResult<IResult>(Results.StatusCode(500))).ConfigureAwait(false);
        })
        .WithName("TriggerRootCauseAnalysis")
        .WithSummary("Trigger AI-powered root cause analysis for an anomaly")
        .Produces<RootCauseAnalysisDto>()
        .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/root-cause-analysis/{id}", async (
            string id,
            RootCauseAnalysisRepository repository,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(id, out var entityId))
            {
                return Results.BadRequest(new { error = "Invalid ID" });
            }

            var entity = await repository.GetByIdAsync(entityId, ct).ConfigureAwait(false);
            if (entity is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(MapToDto(entity));
        })
        .WithName("GetRootCauseAnalysis")
        .WithSummary("Get a root cause analysis by ID")
        .Produces<RootCauseAnalysisDto>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/root-cause-analysis/agent/{agentId}", async (
            string agentId,
            RootCauseAnalysisRepository repository,
            int? limit,
            CancellationToken ct) =>
        {
            var results = await repository.GetByAgentAsync(agentId, limit ?? 50, ct).ConfigureAwait(false);
            return Results.Ok(results.Select(MapToDto).ToList());
        })
        .WithName("GetRootCauseAnalysesByAgent")
        .WithSummary("Get root cause analyses for a specific agent")
        .Produces<IReadOnlyList<RootCauseAnalysisDto>>();

        return app;
    }

    private static RootCauseAnalysisDto MapToDto(RootCauseAnalysis entity)
    {
        var evidence = JsonSerializer.Deserialize<List<string>>(entity.EvidenceJson) ?? new();
        var factors = JsonSerializer.Deserialize<List<string>>(entity.ContributingFactorsJson) ?? new();
        var actions = JsonSerializer.Deserialize<List<RecommendedActionDto>>(entity.RecommendedActionsJson) ?? new();
        var traceIds = JsonSerializer.Deserialize<List<string>>(entity.RelatedTraceIdsJson) ?? new();

        return new RootCauseAnalysisDto(
            entity.Id.ToString(),
            entity.TenantId,
            entity.AgentId,
            entity.TraceId,
            entity.AnomalyType,
            entity.AnomalyStartTime,
            entity.AnomalyEndTime,
            entity.ProbableRootCause,
            entity.Confidence,
            entity.ConfidenceLabel,
            evidence,
            factors,
            actions,
            traceIds,
            entity.Status,
            entity.CreatedAt);
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using MediatR;
using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Application.Commands;
using SeldonVigil.Api.Modules.Observability.Application.Queries;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Observability.Api.Endpoints;

internal static class ObservabilityEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapObservabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/observability")
            .WithTags("Observability")

            .RequireAuthorization();

        // GET /api/v1/observability/traces
        group.MapGet("/traces", async (
                Guid? agentId,
                string? status,
                DateTime? from,
                DateTime? to,
                int? page,
                int? pageSize,
                int? limit,
                ISender mediator,
                CancellationToken ct) =>
            {
                // "limit" kept as a backwards-compatible alias for pageSize.
                var query = new ListTracesQuery(agentId, status, from, to, page ?? 1, pageSize ?? limit ?? 50);
                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(
                    Results.Ok,
                    _ => Results.StatusCode(500));
            })
        .WithName("ListTraces")
        .WithSummary("List traces with filtering and pagination")
        .Produces<IReadOnlyList<TraceDto>>();

        // GET /api/v1/observability/traces/{id}
        group.MapGet("/traces/{id:guid}", async (
                Guid id,
                ISender mediator,
                CancellationToken ct) =>
            {
                var query = new GetTraceByIdQuery(id);
                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(
                    Results.Ok,
                    error => error.Type == ErrorType.NotFound
                        ? Results.NotFound(new { error.Code, error.Message })
                        : Results.StatusCode(500));
            })
        .WithName("GetTrace")
        .WithSummary("Get trace by ID")
        .Produces<TraceDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /api/v1/observability/traces
        group.MapPost("/traces", async (
                IngestTraceRequest request,
                ISender mediator,
                IRequestContext requestContext,
                CancellationToken ct) =>
            {
                if (!Guid.TryParse(request.AgentId, out var agentGuid))
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["agentId"] = ["agentId must be a valid GUID."]
                    });
                }

                if (!Guid.TryParse(requestContext.TenantId, out var tenantGuid))
                {
                    tenantGuid = Guid.Empty;
                }

                var command = new IngestTraceCommand(
                    new TenantId(tenantGuid),
                    new AgentId(agentGuid),
                    ParseOptionalTraceId(request.ParentTraceId),
                    string.IsNullOrWhiteSpace(request.OperationName) ? "unknown" : request.OperationName,
                    ParseEnum(request.OperationType, OperationType.Custom),
                    request.StartTime ?? DateTime.UtcNow.AddSeconds(-1),
                    request.EndTime ?? DateTime.UtcNow,
                    ParseEnum(request.Status, TraceStatus.Ok),
                    CoerceAttributes(request.Attributes),
                    MapEvents(request.Events),
                    [],
                    new TokenUsage(
                        request.TokenUsage?.InputTokens ?? 0,
                        request.TokenUsage?.OutputTokens ?? 0,
                        request.TokenUsage?.CachedTokens ?? 0),
                    new Cost(request.Cost ?? 0m),
                    IsAsync: false,
                    ModelName: request.Attributes?.TryGetValue("model", out var mn) == true ? mn?.ToString() ?? "unknown" : "unknown",
                    Id: ParseOptionalTraceId(request.Id));

                var result = await mediator.Send(command, ct).ConfigureAwait(false);

                return await result.Match(
                    async id =>
                    {
                        // Record spend against agent-scoped budgets so Cost
                        // Intelligence reflects real usage. Best-effort: never
                        // fail ingestion over it.
                        if (request.TokenUsage is not null || request.Cost is not null)
                        {
                            try
                            {
                                await mediator.Send(new SeldonVigil.Api.Modules.CostIntelligence.Application.Commands.RecordTraceCostCommand(
                                    new SeldonVigil.Api.Modules.Identity.Domain.ValueObjects.TenantId(tenantGuid),
                                    id,
                                    new AgentId(agentGuid),
                                    ModelProvider.Custom,
                                    request.Attributes?.TryGetValue("model", out var modelAttr) == true ? modelAttr?.ToString() ?? "unknown" : "unknown",
                                    request.TokenUsage?.InputTokens ?? 0,
                                    request.TokenUsage?.OutputTokens ?? 0,
                                    request.TokenUsage?.CachedTokens ?? 0,
                                    request.Cost ?? 0m), ct).ConfigureAwait(false);
                            }
                            catch
                            {
                                // Cost recording is best-effort only.
                            }
                        }

                        return Results.Created($"/api/v1/observability/traces/{id}", new { traceId = id.ToString() });
                    },
                    error => Task.FromResult(error.Type switch
                    {
                        ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                        ErrorType.Forbidden => Results.Problem(error.Message, statusCode: StatusCodes.Status403Forbidden),
                        ErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["trace"] = [error.Message]
                        }),
                        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
                    })).ConfigureAwait(false);
            })
        .WithName("IngestTrace")
        .WithSummary("Ingest a completed trace for a registered agent")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // POST /api/v1/observability/traces/batch
        group.MapPost("/traces/batch", async (
                List<SeldonVigil.Sdk.Observability.SeldonVigilTrace> traces,
                ISender mediator,
                IRequestContext requestContext,
                CancellationToken ct) =>
            {
                if (!Guid.TryParse(requestContext.TenantId, out var tenantGuid))
                {
                    tenantGuid = Guid.Empty;
                }

                var results = new List<string>();
                var errors = new Dictionary<string, string>();
                var index = 0;

                // Flatten the hierarchy: root traces + all their children
                var flatTraces = traces.SelectMany(FlattenTrace).ToList();

                foreach (var trace in flatTraces)
                {
                    if (!Guid.TryParse(trace.AgentId, out var agentGuid))
                    {
                        errors[$"traces[{index}]"] = "agentId must be a valid GUID.";
                        index++;
                        continue;
                    }

                    var command = new IngestTraceCommand(
                        new TenantId(tenantGuid),
                        new AgentId(agentGuid),
                        ParseOptionalTraceId(trace.ParentTraceId),
                        string.IsNullOrWhiteSpace(trace.OperationName) ? "unknown" : trace.OperationName,
                        ParseEnum(trace.OperationType, OperationType.Custom),
                        trace.StartedAt,
                        trace.EndedAt ?? DateTime.UtcNow,
                        ParseEnum(trace.Status.ToString(), TraceStatus.Ok),
                        CoerceAttributes(trace.Attributes?.ToDictionary(k => k.Key, v => (object?)v.Value)),
                        MapEvents(trace.Events?.Select(e => new IngestTraceEventDto(e.Name, e.Timestamp, e.Attributes?.ToDictionary(k => k.Key, v => (object?)v.Value))).ToList()),
                        [],
                        trace.TokenUsage != null
                            ? new TokenUsage(trace.TokenUsage.InputTokens, trace.TokenUsage.OutputTokens, trace.TokenUsage.CachedTokens)
                            : new TokenUsage(0, 0, 0),
                        new Cost(trace.Cost ?? 0m),
                        IsAsync: false,
                        ModelName: trace.ModelName ?? "unknown",
                        Id: ParseOptionalTraceId(trace.Id));

                    var result = await mediator.Send(command, ct).ConfigureAwait(false);

                    result.Match(
                        id =>
                        {
                            results.Add(id.ToString());
                            return id;
                        },
                        error =>
                        {
                            errors[$"traces[{index}]"] = error.Message;
                            return TraceId.Empty;
                        });

                    index++;
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors.ToDictionary(kvp => kvp.Key, kvp => new[] { kvp.Value }));
                }

                return Results.Ok(new { traceIds = results });
            })
        .WithName("IngestTracesBatch")
        .WithSummary("Ingest multiple traces in a single request")
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        return app;
    }

    private static TraceId? ParseOptionalTraceId(string? value)
        => Guid.TryParse(value, out var guid) ? new TraceId(guid) : null;

    private static IEnumerable<SeldonVigil.Sdk.Observability.SeldonVigilTrace> FlattenTrace(SeldonVigil.Sdk.Observability.SeldonVigilTrace trace)
    {
        yield return trace;
        if (trace.Children is null) yield break;
        foreach (var child in trace.Children)
        {
            foreach (var flattened in FlattenTrace(child))
                yield return flattened;
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out TEnum parsed) ? parsed : fallback;

    private static Dictionary<string, string> CoerceAttributes(Dictionary<string, object?>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return [];
        }

        var result = new Dictionary<string, string>(attributes.Count, StringComparer.Ordinal);
        foreach (var (key, value) in attributes)
        {
            if (value is null)
            {
                continue;
            }

            result[key] = value switch
            {
                string s => s,
                JsonElement { ValueKind: JsonValueKind.String } e => e.GetString() ?? string.Empty,
                JsonElement e => e.GetRawText(),
                _ => value.ToString() ?? string.Empty
            };
        }

        return result;
    }

    private static List<TraceEvent> MapEvents(List<IngestTraceEventDto>? events)
    {
        if (events is null || events.Count == 0)
        {
            return [];
        }

        var result = new List<TraceEvent>(events.Count);
        foreach (var dto in events)
        {
            result.Add(new TraceEvent(
                dto.Name ?? "unknown",
                dto.Timestamp ?? DateTime.UtcNow,
                CoerceAttributes(dto.Attributes)));
        }

        return result;
    }
}

internal sealed record IngestTraceRequest(
    string? Id,
    string AgentId,
    string? ParentTraceId,
    string? OperationName,
    string? OperationType,
    DateTime? StartTime,
    DateTime? EndTime,
    string? Status,
    Dictionary<string, object?>? Attributes,
    List<IngestTraceEventDto>? Events,
    IngestTokenUsageDto? TokenUsage,
    decimal? Cost);

internal sealed record IngestTraceEventDto(
    string? Name,
    DateTime? Timestamp,
    Dictionary<string, object?>? Attributes);

internal sealed record IngestTokenUsageDto(
    int InputTokens,
    int OutputTokens,
    int CachedTokens = 0);

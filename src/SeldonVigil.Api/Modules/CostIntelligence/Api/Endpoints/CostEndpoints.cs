using System.Diagnostics.CodeAnalysis;
using MediatR;
using SeldonVigil.Api.Modules.CostIntelligence.Application.Commands;
using SeldonVigil.Api.Modules.CostIntelligence.Application.Queries;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.CostIntelligence.Api.Endpoints;

internal static class CostEndpoints
{
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapCostEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/costs")
            .WithTags("Cost Intelligence")

            .RequireAuthorization();
        // .AddEndpointFilter<TenantResolutionFilter>();

        // GET /api/v1/costs/dashboard
        group.MapGet("/dashboard", async Task<IResult> (
                [AsParameters] CostDashboardRequest request,
                ISender mediator,
                CancellationToken ct) =>
            {
                var query = new GetCostDashboardQuery(
                    request.From,
                    request.To,
                    request.GroupBy);

                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(
                    dashboard => Results.Ok(dashboard),
                    _ => Results.StatusCode(500));
            })
        .WithName("GetCostDashboard")
        .WithSummary("Get cost dashboard with trends and breakdowns")
        .Produces<CostDashboardDto>();

        // GET /api/v1/costs/recommendations
        group.MapGet("/recommendations", async Task<IResult> (
            Guid? agentId,
            int? limit,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new GetCostOptimizationRecommendationsQuery(
                agentId.HasValue ? new AgentId(agentId.Value) : null,
                limit ?? 10);

            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                recommendations => Results.Ok(recommendations),
                _ => Results.StatusCode(500));
        })
        .WithName("GetCostRecommendations")
        .WithSummary("Get AI-powered cost optimization recommendations")
        .Produces<IReadOnlyList<CostOptimizationRecommendationDto>>();

        // POST /api/v1/costs/budgets
        group.MapPost("/budgets", async Task<IResult> (
            CreateCostBudgetRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new CreateCostBudgetCommand(
                request.Name,
                request.ScopeType,
                request.ScopeId,
                request.Amount,
                request.Currency,
                request.Period,
                request.AlertThresholds,
                request.EnforcementType);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                id => Results.Created($"/api/v1/costs/budgets/{id}", new { Id = id }),
                error => error.Type switch
                {
                    ErrorType.Conflict => Results.Problem(error.Message, statusCode: StatusCodes.Status409Conflict),
                    ErrorType.Validation => Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["budget"] = [error.Message]
                    }),
                    _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
                });
        })
        .WithName("CreateCostBudget")
        .WithSummary("Create a new cost budget")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // GET /api/v1/costs/budgets
        group.MapGet("/budgets", async Task<IResult> (
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new ListCostBudgetsQuery();
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                Results.Ok,
                _ => Results.StatusCode(500));
        })
        .WithName("ListCostBudgets")
        .WithSummary("List all cost budgets")
        .Produces<IReadOnlyList<CostBudgetListItemDto>>();

        return app;
    }
}

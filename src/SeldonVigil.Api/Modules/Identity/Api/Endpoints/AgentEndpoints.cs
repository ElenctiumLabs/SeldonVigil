using System.Diagnostics.CodeAnalysis;
using MediatR;
using SeldonVigil.Api.Modules.Identity.Application.Commands;
using SeldonVigil.Api.Modules.Identity.Application.Queries;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Identity.Api.Endpoints;

internal static class AgentEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/agents")
            .WithTags("Agents")

            .RequireAuthorization();

        // POST /api/v1/agents
        group.MapPost("/", async (
            RegisterAgentRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new RegisterAgentCommand(
                request.ExternalId,
                request.Name,
                request.Description,
                request.Framework,
                request.FrameworkVersion,
                request.ModelProvider,
                request.ModelName,
                request.ModelVersion,
                request.Manifest);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                id => Results.Created($"/api/v1/agents/{id}", new { Id = id.ToString() }),
                error => error.Type switch
                {
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("RegisterAgent")
        .WithSummary("Register a new AI agent")
        .Produces<AgentId>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict);

        // GET /api/v1/agents/{id}
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new GetAgentByIdQuery(new AgentId(id));
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                Results.Ok,
                error => error.Type == ErrorType.NotFound
                    ? Results.NotFound(new { error.Code, error.Message })
                    : Results.StatusCode(500));
        })
        .WithName("GetAgent")
        .WithSummary("Get agent by ID")
        .Produces<AgentDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/v1/agents
        group.MapGet("/", async (
            [AsParameters] ListAgentsRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new ListAgentsQuery(
                request.Status,
                request.Framework,
                request.ModelProvider,
                request.Page,
                request.PageSize);

            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                page => Results.Ok(page),
                _ => Results.StatusCode(500));
        })
        .WithName("ListAgents")
        .WithSummary("List agents with filtering and pagination")
        .Produces<PaginatedResult<AgentListItemDto>>();

        // POST /api/v1/agents/{id}/activate
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new ActivateAgentCommand(new AgentId(id));
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("ActivateAgent")
        .WithSummary("Activate a pending agent")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // POST /api/v1/agents/{id}/suspend
        group.MapPost("/{id:guid}/suspend", async (
            Guid id,
            SuspendAgentRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new SuspendAgentCommand(new AgentId(id), request.Reason);
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    ErrorType.Conflict => Results.Conflict(new { error.Code, error.Message }),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("SuspendAgent")
        .WithSummary("Suspend an active agent")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // POST /api/v1/agents/{id}/revoke
        group.MapPost("/{id:guid}/revoke", async (
            Guid id,
            RevokeAgentRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new RevokeAgentCommand(new AgentId(id), request.Reason);
            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                _ => Results.NoContent(),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("RevokeAgent")
        .WithSummary("Permanently revoke an agent")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}

// Request/Response DTOs

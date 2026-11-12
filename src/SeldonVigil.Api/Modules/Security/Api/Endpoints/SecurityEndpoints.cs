using System.Diagnostics.CodeAnalysis;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SeldonVigil.Api.Modules.Identity.Application.Queries;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Application.Commands;
using SeldonVigil.Api.Modules.Security.Application.Queries;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.SharedKernel.Domain;

namespace SeldonVigil.Api.Modules.Security.Api.Endpoints;

internal static class SecurityEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/security")
            .WithTags("Security")

            .RequireAuthorization();
        // .AddEndpointFilter<TenantResolutionFilter>();

        // POST /api/v1/security/policies - admin only: policy changes affect
        // enforcement behaviour for every agent in the tenant.
        group.MapPost("/policies", async (
            CreateSecurityPolicyRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new CreateSecurityPolicyCommand(
                request.Name,
                request.Description,
                request.Category,
                request.RuleDefinition,
                request.Severity,
                request.AutoResponse);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                id => Results.Created($"/api/v1/security/policies/{id}", new { Id = id }),
                error => error.Type switch
                {
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    ErrorType.Unauthorized => Results.Unauthorized(),
                    _ => Results.StatusCode(500)
                });
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreateSecurityPolicy")
        .WithSummary("Create a new security policy")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // GET /api/v1/security/policies
        group.MapGet("/policies", async (
            [AsParameters] ListSecurityPoliciesRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new ListSecurityPoliciesQuery(
                request.Category,
                request.Status,
                request.Page,
                request.PageSize);

            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                page => Results.Ok(page),
                _ => Results.StatusCode(500));
        })
        .WithName("ListSecurityPolicies")
        .WithSummary("List security policies with filtering")
        .Produces<PaginatedResult<SecurityPolicyListItemDto>>();

        // GET /api/v1/security/policies/{id}
        group.MapGet("/policies/{id:guid}", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new GetSecurityPolicyByIdQuery(new SecurityPolicyId(id));
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                policy => Results.Ok(policy),
                error => error.Type == ErrorType.NotFound
                    ? Results.NotFound(new { error.Code, error.Message })
                    : Results.StatusCode(500));
        })
        .WithName("GetSecurityPolicy")
        .WithSummary("Get security policy by ID")
        .Produces<SecurityPolicyDto>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // PUT /api/v1/security/policies/{id}
        group.MapPut("/policies/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSecurityPolicyRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new UpdateSecurityPolicyCommand(
                new SecurityPolicyId(id),
                request.Name,
                request.Description,
                request.RuleDefinition,
                request.Severity,
                request.AutoResponse);

            var response = await mediator.Send(command, ct).ConfigureAwait(false);
            if (response is not Shared.Domain.Result<SeldonVigil.SharedKernel.Application.Unit> result)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
            return result.Match(
                _ => Results.NoContent(),
                error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(new { error.Code, error.Message }),
                    ErrorType.Validation => Results.BadRequest(new { error.Code, error.Message }),
                    _ => Results.StatusCode(500)
                });
        })
        .WithName("UpdateSecurityPolicy")
        .WithSummary("Update an existing security policy")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // DELETE /api/v1/security/policies/{id}
        group.MapDelete("/policies/{id:guid}", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new DisableSecurityPolicyCommand(new SecurityPolicyId(id));
            var response = await mediator.Send(command, ct).ConfigureAwait(false);
            if (response is not SeldonVigil.Api.Shared.Domain.Result<SeldonVigil.SharedKernel.Application.Unit> result)
            {
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
            return result.Match(
                _ => Results.NoContent(),
                error => error.Type == ErrorType.NotFound
                    ? Results.NotFound(new { error.Code, error.Message })
                    : Results.StatusCode(500));
        })
        .WithName("DisableSecurityPolicy")
        .WithSummary("Disable a security policy")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /api/v1/security/evaluate
        group.MapPost("/evaluate", async (
            EvaluateSecurityRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var command = new EvaluateSecurityPolicyCommand(
                new TraceId(request.TraceId),
                new AgentId(request.AgentId),
                request.Attributes,
                request.OperationName,
                request.OperationType,
                request.CostUsd,
                request.TokenUsageTotal);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                _ => Results.Ok(new { Evaluated = true, Timestamp = DateTime.UtcNow }),
                _ => Results.StatusCode(500));
        })
        .WithName("EvaluateSecurity")
        .WithSummary("Evaluate security policies against a trace")
        .Produces(StatusCodes.Status200OK);

        // GET /api/v1/security/violations
        group.MapGet("/violations", async (
            [AsParameters] ListSecurityViolationsRequest request,
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new ListSecurityViolationsQuery(
                request.AgentId,
                request.Severity,
                request.Status,
                request.From,
                request.To,
                request.Page,
                request.PageSize);

            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                Results.Ok,
                _ => Results.StatusCode(500));
        })
        .WithName("ListSecurityViolations")
        .WithSummary("List security violations with filtering")
        .Produces<PaginatedResult<SecurityViolationDto>>();

        // GET /api/v1/security/dashboard
        group.MapGet("/dashboard", async (
            ISender mediator,
            CancellationToken ct) =>
        {
            var query = new GetSecurityDashboardQuery();
            var result = await mediator.Send(query, ct).ConfigureAwait(false);
            return result.Match(
                Results.Ok,
                _ => Results.StatusCode(500));
        })
        .WithName("GetSecurityDashboard")
        .WithSummary("Get security dashboard metrics")
        .Produces<SecurityDashboardDto>();

        // POST /api/v1/security/evaluate-input
        group.MapPost("/evaluate-input", async (
            EvaluateInputRequest request,
            ISender mediator,
            IRequestContext requestContext,
            CancellationToken ct) =>
        {
            if (!Guid.TryParse(requestContext.TenantId, out var tenantGuid))
            {
                tenantGuid = Guid.Empty;
            }

            var command = new EvaluateInputCommand(
                tenantGuid,
                request.AgentId ?? string.Empty,
                request.OperationType ?? "custom",
                request.Input ?? [],
                request.Context ?? []);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                evaluation => Results.Ok(new
                {
                    isAllowed = evaluation.IsAllowed,
                    violations = evaluation.Violations.Select(v => new
                    {
                        policyId = v.PolicyId,
                        policyName = v.PolicyName,
                        severity = v.Severity,
                        message = v.Message
                    })
                }),
                _ => Results.StatusCode(500));
        })
        .WithName("EvaluateInput")
        .WithSummary("Synchronously evaluate security policies against submitted input content")
        .Produces(StatusCodes.Status200OK);

        return app;
    }
}

internal sealed record EvaluateInputRequest(
    string? AgentId,
    string? OperationType,
    Dictionary<string, string>? Input,
    Dictionary<string, string>? Context);

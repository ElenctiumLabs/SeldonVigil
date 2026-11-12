using System.Diagnostics.CodeAnalysis;
using MediatR;
using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Governance.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Governance.Api.Endpoints;

internal sealed record GovernancePolicyDto(
    string Id,
    string TenantId,
    string Name,
    string Description,
    string Category,
    string DefinitionYaml,
    string Status,
    int Version,
    string? PreviousVersionId,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? DeprecatedAt);

internal sealed record CreatePolicyRequest(
    string Name,
    string Description,
    string Category,
    string DefinitionYaml);

internal sealed record CertificationPipelineDto(
    string Id,
    string TenantId,
    string AgentId,
    string Status,
    string? CurrentStage,
    IReadOnlyList<PipelineStageDto> Stages,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ApprovalNotes);

internal sealed record PipelineStageDto(
    string Name,
    string Description,
    bool IsRequired,
    string Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? Output);

internal sealed record TriggerCertificationRequest(string AgentId);
internal sealed record ApproveCertificationRequest(string? Notes);

internal sealed record CreatePolicyCommand(
    string TenantId,
    string Name,
    string Description,
    string Category,
    string DefinitionYaml,
    Guid CreatedBy) : ICommand<GovernancePolicyDto>;

internal sealed class CreatePolicyCommandHandler(
    GovernancePolicyRepository repository,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreatePolicyCommand, GovernancePolicyDto>
{
    public async Task<Result<GovernancePolicyDto>> Handle(CreatePolicyCommand command, CancellationToken cancellationToken)
    {
        var tenantId = new TenantId(Guid.Parse(command.TenantId));
        if (!Enum.TryParse<PolicyCategory>(command.Category, ignoreCase: true, out var category))
        {
            category = PolicyCategory.Operational;
        }

        var result = GovernancePolicy.Create(
            tenantId, command.Name, command.Description, category,
            command.DefinitionYaml, command.CreatedBy, dateTimeProvider);

        if (result.IsFailure)
        {
            return Result.Failure<GovernancePolicyDto>(result.Error!);
        }

        var policy = result.Value!;
        await repository.AddAsync(policy, cancellationToken);

        return Result.Success(GovernanceEndpoints.MapPolicyToDto(policy));
    }
}

internal static class GovernanceEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/governance")
            .WithTags("Governance")

            .RequireAuthorization();

        var policyGroup = group.MapGroup("/policies")
            .WithTags("Governance Policies");

        policyGroup.MapGet("/", async (
            IRequestContext requestContext,
            GovernancePolicyRepository repository,
            CancellationToken ct) =>
        {
            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return Results.BadRequest(new { error = "Tenant required" });

            var policies = await repository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false);
            return Results.Ok(policies.Select(MapPolicyToDto).ToList());
        })
        .WithName("ListPolicies")
        .WithSummary("List governance policies for the tenant");

        policyGroup.MapGet("/{id:guid}", async (
            Guid id,
            GovernancePolicyRepository repository,
            CancellationToken ct) =>
        {
            var policy = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (policy is null) return Results.NotFound();
            return Results.Ok(MapPolicyToDto(policy));
        })
        .WithName("GetPolicy")
        .WithSummary("Get a governance policy by ID");

        policyGroup.MapPost("/", async (
            CreatePolicyRequest request,
            ISender mediator,
            IRequestContext requestContext,
            CancellationToken ct) =>
        {
            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return Results.BadRequest(new { error = "Tenant required" });

            var userId = requestContext.UserId;
            if (!userId.HasValue) return Results.BadRequest(new { error = "User required" });

            var command = new CreatePolicyCommand(
                tenantId, request.Name, request.Description, request.Category,
                request.DefinitionYaml, userId.Value);

            var result = await mediator.Send(command, ct).ConfigureAwait(false);
            return result.Match(
                dto => Results.Created($"/api/v1/governance/policies/{dto.Id}", dto),
                e => Results.BadRequest(new { error = e }));
        })
        .WithName("CreatePolicy")
        .WithSummary("Create a new governance policy");

        policyGroup.MapPost("/{id:guid}/activate", async (
            Guid id,
            GovernancePolicyRepository repository,
            IRequestContext requestContext,
            IDateTimeProvider dateTimeProvider,
            CancellationToken ct) =>
        {
            var policy = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (policy is null) return Results.NotFound();

            var userId = requestContext.UserId;
            if (!userId.HasValue) return Results.BadRequest(new { error = "User required" });

            var result = policy.Activate(userId.Value, dateTimeProvider);
            if (result.IsSuccess)
            {
                await repository.SaveAsync(policy, ct).ConfigureAwait(false);
            }
            return result.Match(
                _ => Results.Ok(MapPolicyToDto(policy)),
                e => Results.BadRequest(new { error = e }));
        })
        .WithName("ActivatePolicy")
        .WithSummary("Activate a draft governance policy");

        var certGroup = group.MapGroup("/certification")
            .WithTags("Certification Pipelines");

        certGroup.MapPost("/trigger", async (
            TriggerCertificationRequest request,
            IRequestContext requestContext,
            CertificationPipelineRepository repository,
            IDateTimeProvider dateTimeProvider,
            CancellationToken ct) =>
        {
            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return Results.BadRequest(new { error = "Tenant required" });

            var userId = requestContext.UserId;
            if (!userId.HasValue) return Results.BadRequest(new { error = "User required" });

            var agentId = new AgentId(Guid.Parse(request.AgentId));
            var pipeline = CertificationPipeline.Create(
                new TenantId(Guid.Parse(tenantId)), agentId, userId.Value, dateTimeProvider);

            await repository.AddAsync(pipeline, ct);

            return Results.Created($"/api/v1/governance/certification/{pipeline.Id}", MapPipelineToDto(pipeline));
        })
        .WithName("TriggerCertification")
        .WithSummary("Trigger a certification pipeline for an agent");

        certGroup.MapGet("/{id:guid}", async (
            Guid id,
            CertificationPipelineRepository repository,
            CancellationToken ct) =>
        {
            var pipeline = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (pipeline is null) return Results.NotFound();
            return Results.Ok(MapPipelineToDto(pipeline));
        })
        .WithName("GetCertificationPipeline")
        .WithSummary("Get a certification pipeline by ID");

        certGroup.MapGet("/agent/{agentId}", async (
            string agentId,
            CertificationPipelineRepository repository,
            CancellationToken ct) =>
        {
            var pipelines = await repository.GetByAgentAsync(agentId, ct).ConfigureAwait(false);
            return Results.Ok(pipelines.Select(MapPipelineToDto).ToList());
        })
        .WithName("GetPipelinesByAgent")
        .WithSummary("Get certification pipelines for an agent");

        certGroup.MapPost("/{id:guid}/approve", async (
            Guid id,
            ApproveCertificationRequest request,
            CertificationPipelineRepository repository,
            IRequestContext requestContext,
            IDateTimeProvider dateTimeProvider,
            CancellationToken ct) =>
        {
            var pipeline = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (pipeline is null) return Results.NotFound();

            var userId = requestContext.UserId;
            if (!userId.HasValue) return Results.BadRequest(new { error = "User required" });

            var result = pipeline.Approve(userId.Value, request.Notes, dateTimeProvider);
            if (result.IsSuccess)
            {
                await repository.SaveAsync(pipeline, ct).ConfigureAwait(false);
            }
            return result.Match(
                _ => Results.Ok(MapPipelineToDto(pipeline)),
                e => Results.BadRequest(new { error = e }));
        })
        .WithName("ApproveCertification")
        .WithSummary("Approve a certification pipeline");

        return app;
    }

    internal static GovernancePolicyDto MapPolicyToDto(GovernancePolicy p) => new(
        p.Id.ToString(), p.TenantId.Value.ToString(), p.Name, p.Description,
        p.Category.ToString(), p.DefinitionYaml, p.Status.ToString(),
        p.Version, p.PreviousVersionId, p.CreatedAt, p.ActivatedAt, p.DeprecatedAt);

    private static CertificationPipelineDto MapPipelineToDto(CertificationPipeline p) => new(
        p.Id.ToString(), p.TenantId.Value.ToString(), p.AgentId.Value.ToString(),
        p.Status.ToString(), p.CurrentStage,
        p.Stages.Select(s => new PipelineStageDto(
            s.Name, s.Description, s.IsRequired, s.Status.ToString(),
            s.StartedAt, s.CompletedAt, s.Output)).ToList(),
        p.StartedAt, p.CompletedAt, p.ApprovalNotes);
}

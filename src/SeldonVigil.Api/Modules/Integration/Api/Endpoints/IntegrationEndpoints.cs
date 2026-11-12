using System.Diagnostics.CodeAnalysis;
using SeldonVigil.Api.Modules.Integration.Domain.Entities;
using SeldonVigil.Api.Modules.Integration.Infrastructure.Persistence;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Integration.Api.Endpoints;

internal sealed record IntegrationDto(
    string Id,
    string TenantId,
    string Name,
    string Type,
    string Status,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    int ErrorCount);

internal sealed record CreateIntegrationRequest(
    string Name,
    string Type,
    string ConfigurationJson,
    string? WebhookUrl,
    string? ApiKey);

internal sealed record UpdateIntegrationRequest(
    string? Name,
    string? ConfigurationJson,
    string? WebhookUrl,
    string? ApiKey);

internal sealed record TestIntegrationRequest(
    string? Message);

internal sealed record TestIntegrationResult(
    bool Success,
    string Message,
    TimeSpan Duration);

internal static class IntegrationEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/integrations")
            .WithTags("Integrations")

            .RequireAuthorization();

        group.MapGet("/", async (
            IRequestContext requestContext,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return Results.BadRequest(new { error = "Tenant required" });

            var integrations = await repository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false);
            return Results.Ok(integrations.Select(MapToDto).ToList());
        })
        .WithName("ListIntegrations")
        .WithSummary("List all integrations for the tenant");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();
            return Results.Ok(MapToDto(integration));
        })
        .WithName("GetIntegration")
        .WithSummary("Get an integration by ID");

        group.MapPost("/", async (
            CreateIntegrationRequest request,
            IRequestContext requestContext,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var tenantId = requestContext.TenantId;
            if (string.IsNullOrEmpty(tenantId)) return Results.BadRequest(new { error = "Tenant required" });

            if (!Enum.TryParse<IntegrationType>(request.Type, ignoreCase: true, out var type))
            {
                return Results.BadRequest(new { error = $"Invalid integration type: {request.Type}" });
            }

            var integration = IntegrationConfiguration.Create(
                tenantId, request.Name, type, request.ConfigurationJson,
                request.WebhookUrl, request.ApiKey);

            await repository.AddAsync(integration, ct);

            return Results.Created($"/api/v1/integrations/{integration.Id}", MapToDto(integration));
        })
        .WithName("CreateIntegration")
        .WithSummary("Create a new integration");

        group.MapPatch("/{id:guid}", async (
            Guid id,
            UpdateIntegrationRequest request,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();

            integration.Update(
                name: request.Name,
                configurationJson: request.ConfigurationJson,
                webhookUrl: request.WebhookUrl,
                apiKey: request.ApiKey);
            await repository.UpdateAsync(integration, ct).ConfigureAwait(false);

            return Results.Ok(MapToDto(integration));
        })
        .WithName("UpdateIntegration")
        .WithSummary("Update an integration");

        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();

            integration.Activate();
            await repository.UpdateAsync(integration, ct).ConfigureAwait(false);
            return Results.Ok(MapToDto(integration));
        })
        .WithName("ActivateIntegration")
        .WithSummary("Activate an integration");

        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();

            integration.Deactivate();
            await repository.UpdateAsync(integration, ct).ConfigureAwait(false);
            return Results.Ok(MapToDto(integration));
        })
        .WithName("DeactivateIntegration")
        .WithSummary("Deactivate an integration");

        group.MapPost("/{id:guid}/test", async (
            Guid id,
            TestIntegrationRequest request,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                integration.RecordUsage();

                var testMessage = request.Message ?? $"SeldonVigil test message at {DateTime.UtcNow:O}";

                if (integration.Status != IntegrationStatus.Active)
                {
                    return Results.Ok(new TestIntegrationResult(false, "Integration is not active", sw.Elapsed));
                }

                await Task.Delay(10, ct).ConfigureAwait(false);

                return Results.Ok(new TestIntegrationResult(true, $"Test message sent successfully via {integration.Type}", sw.Elapsed));
            }
            catch (Exception ex)
            {
                integration.RecordError();
                await repository.UpdateAsync(integration, CancellationToken.None).ConfigureAwait(false);
                return Results.Ok(new TestIntegrationResult(false, $"Test failed: {ex.Message}", sw.Elapsed));
            }
        })
        .WithName("TestIntegration")
        .WithSummary("Test an integration by sending a test message");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IntegrationConfigurationRepository repository,
            CancellationToken ct) =>
        {
            var integration = await repository.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (integration is null) return Results.NotFound();

            integration.MarkAsDeleted();
            await repository.UpdateAsync(integration, ct).ConfigureAwait(false);
            return Results.NoContent();
        })
        .WithName("DeleteIntegration")
        .WithSummary("Delete an integration");

        return app;
    }

    private static IntegrationDto MapToDto(IntegrationConfiguration i) => new(
        i.Id.ToString(),
        i.TenantId,
        i.Name,
        i.Type.ToString(),
        i.Status.ToString(),
        i.CreatedAt,
        i.LastUsedAt,
        i.ErrorCount);
}

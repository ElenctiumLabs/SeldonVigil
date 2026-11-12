using System.Diagnostics.CodeAnalysis;
using MediatR;
using SeldonVigil.Api.Shared.Application;
using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Modules.Domain.Api.Endpoints;

internal sealed record AppSettingsDto(
    string Theme,
    NotificationSettingsDto Notifications,
    ApiSettingsDto Api,
    BudgetSettingsDto Budget);

internal sealed record NotificationSettingsDto(
    bool Email,
    bool Slack,
    bool SecurityAlerts,
    bool CostAlerts);

internal sealed record ApiSettingsDto(
    int PollingInterval,
    int Timeout);

internal sealed record BudgetSettingsDto(
    decimal MonthlyLimit,
    bool AlertsEnabled);

internal sealed record GetSettingsQuery : IQuery<AppSettingsDto>;

internal sealed class GetSettingsQueryHandler : IQueryHandler<GetSettingsQuery, AppSettingsDto>
{
    [RequiresDynamicCode("Creates mock settings data for development")]
    public Task<Result<AppSettingsDto>> Handle(
        GetSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var mockSettings = new AppSettingsDto(
            "system",
            new NotificationSettingsDto(true, true, true, true),
            new ApiSettingsDto(30000, 10000),
            new BudgetSettingsDto(5000m, true));

        return Task.FromResult(Result<AppSettingsDto>.Success(mockSettings));
    }
}

internal static class SettingsEndpoints
{
    [RequiresUnreferencedCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    [RequiresDynamicCode("Calls Microsoft.AspNetCore.Builder.OpenApiEndpointConventionBuilderExtensions.WithOpenApi<TBuilder>()")]
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .WithTags("Settings")

            .RequireAuthorization();

        group.MapGet("/", async (ISender mediator, CancellationToken ct) =>
            {
                var query = new GetSettingsQuery();
                var result = await mediator.Send(query, ct).ConfigureAwait(false);
                return result.Match(Results.Ok, _ => Results.StatusCode(500));
            })
        .WithName("GetSettings")
        .WithSummary("Get application settings")
        .Produces<AppSettingsDto>();

        return app;
    }
}

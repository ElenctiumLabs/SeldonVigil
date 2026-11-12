using SeldonVigil.Api.Modules.Observability.Infrastructure;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Observability;

internal static class ObservabilityModule
{
    public static IServiceCollection AddSeldonVigilObservabilityModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Repositories (DbContext registered centrally in ServiceRegistration)
        _ = services.AddScoped<ITraceStore, PostgresTraceStore>();
        _ = services.AddScoped<ITraceEventStore, PostgresTraceStore>();
        _ = services.AddSingleton<ITraceEnrichmentService, TraceEnrichmentService>();

        return services;
    }
}

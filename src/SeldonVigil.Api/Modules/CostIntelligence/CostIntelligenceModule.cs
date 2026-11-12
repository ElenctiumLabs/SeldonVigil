using SeldonVigil.Api.Modules.CostIntelligence.Domain.Repositories;
using SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.CostIntelligence;

internal static class CostIntelligenceModule
{
    public static IServiceCollection AddSeldonVigilCostIntelligenceModule(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Repositories (DbContext registered centrally in ServiceRegistration)
        services.AddScoped<ICostBudgetRepository, CostBudgetRepository>();
        services.AddScoped<IModelPricingRepository, ModelPricingRepository>();

        return services;
    }
}

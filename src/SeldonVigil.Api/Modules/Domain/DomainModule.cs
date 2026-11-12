using System.Diagnostics.CodeAnalysis;
using Npgsql;
using SeldonVigil.Api.Modules.Domain.Agents;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain;

internal static class DomainModule
{
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    [RequiresDynamicCode("Calls Npgsql.NpgsqlCommand.ExecuteReader()")]
    public static IServiceCollection AddSeldonVigilDomain(this IServiceCollection services)
    {
        // Tool infrastructure
        services.AddSingleton<ToolOrchestrator>();
        services.AddSingleton<IToolOrchestrator>(sp => sp.GetRequiredService<ToolOrchestrator>());
        services.AddSingleton<ToolFunctionAdapter>();

        // Repositories (PostgreSQL-backed so prompts and evaluations survive restarts)
        services.AddScoped<IPromptRepository, PostgresPromptRepository>();
        services.AddScoped<IEvaluationRepository, PostgresEvaluationRepository>();

        // Prompt management
        services.AddScoped<IPromptManager, PromptManager>();

        // Evaluation
        services.AddScoped<IEvaluationService, EvaluationService>();

        // Cost tracking (PostgreSQL-backed)
        services.AddSingleton<ICostTracker, PostgresCostTracker>();

        // Memory (long-term) - needs NpgsqlDataSource for episodic memory
        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("Connection string 'PostgreSQL' not found.");
            return NpgsqlDataSource.Create(connectionString);
        });
        services.AddSingleton<IMemoryService, MemoryService>();

        // Model routing
        services.AddSingleton<IModelRouter, IntelligentModelRouter>();

        // Time series
        services.AddSingleton<ITimeSeriesAnalyzer, TimeSeriesAnalyzer>();

        // Reasoning agents
        services.AddScoped<RootCauseAnalysisAgent>();
        services.AddScoped<PredictiveGovernanceAgent>();

        return services;
    }
}

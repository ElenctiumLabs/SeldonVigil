using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.CostIntelligence;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Budget;

internal static class BudgetListCommand
{
    public static Command Create()
    {
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "table",
        };

        var command = new Command("list", "List all cost budgets")
        {
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var output = parseResult.GetValue(outputOption)!;
            return await ExecuteAsync(output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string output)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var services = BuildServiceProvider(config);
        var costClient = services.GetRequiredService<CostIntelligenceClient>();

        try
        {
            var budgets = await costClient.ListBudgetsAsync().ConfigureAwait(false);

            if (budgets.Count == 0)
            {
                OutputFormatter.Warning("No budgets found.");
                return 0;
            }

            OutputFormatter.Write(budgets, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to list budgets: {ex.Message}");
            return 1;
        }
    }

    private static ServiceProvider BuildServiceProvider(ConfigManager config)
    {
        var services = new ServiceCollection();
        services.AddSeldonVigilSdk(
            auth => { auth.ApiKey = config.ApiKey; auth.BaseUrl = config.BaseUrl ?? "https://localhost:5001"; },
            cfg => { cfg.BaseUrl = config.BaseUrl ?? "https://localhost:5001"; });
        return services.BuildServiceProvider();
    }
}

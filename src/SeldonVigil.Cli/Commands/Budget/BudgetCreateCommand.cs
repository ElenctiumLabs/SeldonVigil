using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.CostIntelligence;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Budget;

internal static class BudgetCreateCommand
{
    public static Command Create()
    {
        var nameOption = new Option<string>("--name")
        {
            Description = "Budget name",
        };
        var amountOption = new Option<decimal>("--amount")
        {
            Description = "Budget amount",
        };
        var currencyOption = new Option<string>("--currency")
        {
            Description = "Currency code",
            DefaultValueFactory = _ => "USD",
        };
        var periodOption = new Option<string?>("--period")
        {
            Description = "Budget period (Monthly, Quarterly, Yearly)",
        };

        var command = new Command("create", "Create a new cost budget")
        {
            nameOption,
            amountOption,
            currencyOption,
            periodOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var amount = parseResult.GetValue(amountOption);
            var currency = parseResult.GetValue(currencyOption)!;
            var period = parseResult.GetValue(periodOption);
            return await ExecuteAsync(name, amount, currency, period).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string name, decimal amount, string currency, string? period)
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
            var budgetId = await costClient.CreateBudgetAsync(new CreateBudgetRequest
            {
                Name = name,
                Amount = amount,
                Currency = currency,
                Period = period,
            }).ConfigureAwait(false);

            OutputFormatter.Success($"Budget '{name}' created with ID: {budgetId}");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to create budget: {ex.Message}");
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

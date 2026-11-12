using System.CommandLine;
using System.Net.Http.Json;
using SeldonVigil.Cli.Infrastructure;

namespace SeldonVigil.Cli.Commands.Budget;

internal static class BudgetResetCommand
{
    public static Command Create()
    {
        var idOption = new Option<string>("--id")
        {
            Description = "Budget ID to reset",
        };

        var command = new Command("reset", "Reset a cost budget spend counter")
        {
            idOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idOption)!;
            return await ExecuteAsync(id).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string id)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        try
        {
            // Budget reset uses a direct API call since the SDK doesn't expose this endpoint yet.
            // This is intentionally kept as a raw HTTP call for the one-off endpoint.
            using var http = new HttpClient
            {
                BaseAddress = new Uri(config.BaseUrl ?? "https://localhost:5001"),
            };
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.ApiKey);
            var response = await http.PostAsJsonAsync($"api/v1/costs/budgets/{id}/reset", new { }).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OutputFormatter.Success($"Budget '{id}' spend counter reset.");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to reset budget: {ex.Message}");
            return 1;
        }
    }
}

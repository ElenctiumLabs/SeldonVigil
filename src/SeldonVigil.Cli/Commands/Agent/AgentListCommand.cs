using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Agent;

internal static class AgentListCommand
{
    public static Command Create()
    {
        var statusOption = new Option<string?>("--status")
        {
            Description = "Filter by status (Active, Suspended, Draft, etc.)",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "table",
        };

        var command = new Command("list", "List all registered agents")
        {
            statusOption,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var status = parseResult.GetValue(statusOption);
            var output = parseResult.GetValue(outputOption)!;
            return await ExecuteAsync(status, output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string? status, string output)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var services = BuildServiceProvider(config);
        var agentClient = services.GetRequiredService<AgentClient>();

        try
        {
            AgentStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<AgentStatus>(status, ignoreCase: true, out var parsed))
            {
                statusFilter = parsed;
            }

            var agents = await agentClient.GetAgentsAsync(statusFilter).ConfigureAwait(false);

            if (agents.Count == 0)
            {
                OutputFormatter.Warning("No agents found.");
                return 0;
            }

            OutputFormatter.Write(agents, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to list agents: {ex.Message}");
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

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Agent;

internal static class AgentGetCommand
{
    public static Command Create()
    {
        var idArg = new Argument<string>("id")
        {
            Description = "Agent ID",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "table",
        };

        var command = new Command("get", "Get agent details")
        {
            idArg,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg)!;
            var output = parseResult.GetValue(outputOption)!;
            return await ExecuteAsync(id, output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string id, string output)
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
            var agent = await agentClient.GetAgentAsync(id).ConfigureAwait(false);
            if (agent is null)
            {
                OutputFormatter.Error($"Agent {id} not found.");
                return 1;
            }

            OutputFormatter.Write(agent, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to get agent: {ex.Message}");
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

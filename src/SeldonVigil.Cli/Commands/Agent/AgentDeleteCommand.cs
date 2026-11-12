using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Agent;

internal static class AgentDeleteCommand
{
    public static Command Create()
    {
        var idArg = new Argument<string>("id")
        {
            Description = "Agent ID",
        };
        var reasonOption = new Option<string>("--reason")
        {
            Description = "Revocation reason",
            DefaultValueFactory = _ => "Revoked via CLI",
        };

        var command = new Command("delete", "Permanently revoke an agent")
        {
            idArg,
            reasonOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArg)!;
            var reason = parseResult.GetValue(reasonOption)!;
            return await ExecuteAsync(id, reason).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string id, string reason)
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
            var agent = await agentClient.RevokeAgentAsync(id, reason).ConfigureAwait(false);
            OutputFormatter.Success($"Agent {agent.Id} revoked. Status: {agent.Status}");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to delete agent: {ex.Message}");
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

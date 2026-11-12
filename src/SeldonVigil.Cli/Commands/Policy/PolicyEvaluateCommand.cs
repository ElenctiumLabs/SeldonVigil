using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Security;

namespace SeldonVigil.Cli.Commands.Policy;

internal static class PolicyEvaluateCommand
{
    public static Command Create()
    {
        var agentIdOption = new Option<string?>("--agent-id")
        {
            Description = "Agent ID to evaluate against",
        };
        var inputOption = new Option<string>("--input")
        {
            Description = "Input content to evaluate (JSON)",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "json",
        };

        var command = new Command("evaluate", "Evaluate input against active security policies")
        {
            agentIdOption,
            inputOption,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var agentId = parseResult.GetValue(agentIdOption);
            var input = parseResult.GetValue(inputOption)!;
            var output = parseResult.GetValue(outputOption)!;
            return await ExecuteAsync(agentId, input, output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string? agentId, string input, string output)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var services = BuildServiceProvider(config);
        var securityClient = services.GetRequiredService<SecurityClient>();

        try
        {
            var evaluation = await securityClient.EvaluatePolicyAsync(new PolicyEvaluation
            {
                AgentId = agentId,
                OperationType = "custom",
                Input = new Dictionary<string, object> { ["content"] = input },
            }).ConfigureAwait(false);

            OutputFormatter.Write(evaluation, output);
            return evaluation.IsAllowed ? 0 : 1;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to evaluate: {ex.Message}");
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

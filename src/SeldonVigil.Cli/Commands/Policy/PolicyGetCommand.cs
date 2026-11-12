using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Security;

namespace SeldonVigil.Cli.Commands.Policy;

internal static class PolicyGetCommand
{
    public static Command Create()
    {
        var idArgument = new Argument<string>("id")
        {
            Description = "Policy ID",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "json",
        };

        var command = new Command("get", "Get a security policy by ID")
        {
            idArgument,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArgument)!;
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
        var securityClient = services.GetRequiredService<SecurityClient>();

        try
        {
            var policy = await securityClient.GetPolicyAsync(id).ConfigureAwait(false);
            if (policy is null)
            {
                OutputFormatter.Warning($"Policy '{id}' not found.");
                return 0;
            }

            OutputFormatter.Write(policy, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to get policy: {ex.Message}");
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

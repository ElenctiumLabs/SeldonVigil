using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Security;

namespace SeldonVigil.Cli.Commands.Policy;

internal static class PolicyDisableCommand
{
    public static Command Create()
    {
        var idArgument = new Argument<string>("id")
        {
            Description = "Policy ID to disable",
        };
        var reasonOption = new Option<string?>("--reason")
        {
            Description = "Reason for disabling",
        };

        var command = new Command("disable", "Disable a security policy")
        {
            idArgument,
            reasonOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var id = parseResult.GetValue(idArgument)!;
            var reason = parseResult.GetValue(reasonOption);
            return await ExecuteAsync(id, reason).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string id, string? reason)
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
            await securityClient.DisablePolicyAsync(id).ConfigureAwait(false);
            OutputFormatter.Success($"Policy '{id}' disabled.");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to disable policy: {ex.Message}");
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

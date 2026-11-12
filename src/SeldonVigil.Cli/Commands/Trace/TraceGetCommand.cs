using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Observability;

namespace SeldonVigil.Cli.Commands.Trace;

internal static class TraceGetCommand
{
    public static Command Create()
    {
        var idArg = new Argument<string>("id")
        {
            Description = "Trace ID",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "table",
        };

        var command = new Command("get", "Get trace details")
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
        var obsClient = services.GetRequiredService<ObservabilityClient>();

        try
        {
            var trace = await obsClient.GetTraceAsync(id).ConfigureAwait(false);
            if (trace is null)
            {
                OutputFormatter.Error($"Trace {id} not found.");
                return 1;
            }

            OutputFormatter.Write(trace, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to get trace: {ex.Message}");
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

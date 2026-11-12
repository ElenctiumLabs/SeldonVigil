using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Observability;

namespace SeldonVigil.Cli.Commands.Trace;

internal static class TraceListCommand
{
    public static Command Create()
    {
        var agentOption = new Option<string?>("--agent")
        {
            Description = "Filter by agent ID",
        };
        var statusOption = new Option<string?>("--status")
        {
            Description = "Filter by status (Ok, Error, Degraded)",
        };
        var limitOption = new Option<int?>("--limit")
        {
            Description = "Maximum number of traces to return",
        };
        var fromOption = new Option<string?>("--from")
        {
            Description = "Start date (ISO 8601)",
        };
        var toOption = new Option<string?>("--to")
        {
            Description = "End date (ISO 8601)",
        };
        var outputOption = new Option<string>("--output")
        {
            Description = "Output format: table, json, yaml",
            DefaultValueFactory = _ => "table",
        };

        var command = new Command("list", "List traces with optional filters")
        {
            agentOption,
            statusOption,
            limitOption,
            fromOption,
            toOption,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var agent = parseResult.GetValue(agentOption);
            var status = parseResult.GetValue(statusOption);
            var limit = parseResult.GetValue(limitOption);
            var from = parseResult.GetValue(fromOption);
            var to = parseResult.GetValue(toOption);
            var output = parseResult.GetValue(outputOption)!;
            return await ExecuteAsync(agent, status, limit, from, to, output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string? agent, string? status, int? limit, string? from, string? to, string output)
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
            var query = new TraceQuery
            {
                AgentId = agent,
                Limit = limit,
            };

            if (!string.IsNullOrWhiteSpace(from) && DateTime.TryParse(from, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fromDate))
                query.StartTime = fromDate;

            if (!string.IsNullOrWhiteSpace(to) && DateTime.TryParse(to, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var toDate))
                query.EndTime = toDate;

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TraceStatus>(status, ignoreCase: true, out var s))
                query.Status = s;

            var traces = await obsClient.QueryTracesAsync(query).ConfigureAwait(false);

            if (traces.Count == 0)
            {
                OutputFormatter.Warning("No traces found.");
                return 0;
            }

            OutputFormatter.Write(traces, output);
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to list traces: {ex.Message}");
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

using System.CommandLine;
using System.Net.Http.Headers;
using SeldonVigil.Cli.Infrastructure;

namespace SeldonVigil.Cli.Commands.Trace;

internal static class TraceExportCommand
{
    public static Command Create()
    {
        var agentIdOption = new Option<string?>("--agent-id")
        {
            Description = "Filter by agent ID",
        };
        var statusOption = new Option<string?>("--status")
        {
            Description = "Filter by status (ok, error, degraded)",
        };
        var fromOption = new Option<DateTime?>("--from")
        {
            Description = "Start date (ISO 8601)",
        };
        var toOption = new Option<DateTime?>("--to")
        {
            Description = "End date (ISO 8601)",
        };
        var formatOption = new Option<string>("--format")
        {
            Description = "Export format: json, csv",
            DefaultValueFactory = _ => "json",
        };
        var outputOption = new Option<string?>("--output")
        {
            Description = "Output file path (stdout if not specified)",
        };

        var command = new Command("export", "Export traces to a file")
        {
            agentIdOption,
            statusOption,
            fromOption,
            toOption,
            formatOption,
            outputOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var agentId = parseResult.GetValue(agentIdOption);
            var status = parseResult.GetValue(statusOption);
            var from = parseResult.GetValue(fromOption);
            var to = parseResult.GetValue(toOption);
            var format = parseResult.GetValue(formatOption)!;
            var output = parseResult.GetValue(outputOption);
            return await ExecuteAsync(agentId, status, from, to, format, output).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(
        string? agentId, string? status, DateTime? from, DateTime? to, string format, string? output)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        try
        {
            using var client = BuildHttpClient(config);
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(agentId)) queryParams.Add($"agentId={agentId}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={status}");
            if (from.HasValue) queryParams.Add($"from={from.Value:O}");
            if (to.HasValue) queryParams.Add($"to={to.Value:O}");
            queryParams.Add("pageSize=1000");

            var query = string.Join("&", queryParams);
            var response = await client.GetAsync(new Uri($"api/v1/observability/traces?{query}", UriKind.Relative)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (format == "csv")
            {
                var csv = ConvertToCsv(json);
                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, csv).ConfigureAwait(false);
                    OutputFormatter.Success($"Traces exported to {output}");
                }
                else
                {
                    Console.WriteLine(csv);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(output))
                {
                    await File.WriteAllTextAsync(output, json).ConfigureAwait(false);
                    OutputFormatter.Success($"Traces exported to {output}");
                }
                else
                {
                    Console.WriteLine(json);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to export traces: {ex.Message}");
            return 1;
        }
    }

    private static string ConvertToCsv(string json)
    {
        var traces = System.Text.Json.JsonSerializer.Deserialize<List<TraceExportDto>>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,AgentId,OperationName,Status,DurationMs,Cost,StartedAt");

        foreach (var t in traces)
        {
            csv.AppendLine($"{t.Id},{t.AgentId},{Escape(t.OperationName)},{t.Status},{t.DurationMs},{t.CostUsd},{t.StartedAt:O}");
        }

        return csv.ToString();
    }

    private static string Escape(string? value) => string.IsNullOrEmpty(value) ? "" : $"\"{value.Replace("\"", "\"\"")}\"";

    private static HttpClient BuildHttpClient(ConfigManager config)
    {
        return new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrl ?? "https://localhost:5001"),
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey),
            },
        };
    }

    private sealed class TraceExportDto
    {
        public string Id { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string OperationName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double DurationMs { get; set; }
        public decimal CostUsd { get; set; }
        public DateTime StartedAt { get; set; }
    }
}

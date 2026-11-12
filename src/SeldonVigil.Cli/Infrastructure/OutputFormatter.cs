using System.Text.Json;
using Spectre.Console;

namespace SeldonVigil.Cli.Infrastructure;

internal static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Write<T>(T data, string format)
    {
        switch (string.Intern(format.ToUpperInvariant()))
        {
            case "JSON":
                WriteJson(data);
                break;
            case "YAML":
                WriteYaml(data);
                break;
            default:
                WriteTable(data);
                break;
        }
    }

    public static void WriteJson<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        AnsiConsole.WriteLine(json);
    }

    public static void WriteTable<T>(T data)
    {
        if (data is IEnumerable<object> enumerable && enumerable.Any())
        {
            WriteEnumerableAsTable(enumerable);
            return;
        }

        WriteObjectAsTable(data);
    }

    public static void WriteYaml<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var doc = JsonDocument.Parse(json);
        WriteJsonAsYaml(doc.RootElement, 0);
    }

    private static void WriteJsonAsYaml(JsonElement element, int indent)
    {
        var prefix = new string(' ', indent * 2);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        AnsiConsole.WriteLine($"{prefix}{prop.Name}:");
                        WriteJsonAsYaml(prop.Value, indent + 1);
                    }
                    else
                    {
                        AnsiConsole.WriteLine($"{prefix}{prop.Name}: {FormatYamlValue(prop.Value)}");
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        AnsiConsole.WriteLine($"{prefix}-");
                        WriteJsonAsYaml(item, indent + 1);
                    }
                    else
                    {
                        AnsiConsole.WriteLine($"{prefix}- {FormatYamlValue(item)}");
                    }
                }
                break;
        }
    }

    private static string FormatYamlValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => $"\"{element.GetString()}\"",
        JsonValueKind.Null => "null",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => element.ToString(),
    };

    private static void WriteEnumerableAsTable(IEnumerable<object> items)
    {
        var table = new Table().Border(TableBorder.Rounded);
        var first = true;

        foreach (var item in items)
        {
            if (first)
            {
                var props = item.GetType().GetProperties();
                foreach (var prop in props)
                {
                    table.AddColumn(new TableColumn($"[bold]{prop.Name}[/]").NoWrap());
                }
                first = false;
            }

            var values = item.GetType().GetProperties().Select(p =>
            {
                var val = p.GetValue(item);
                return val?.ToString() ?? "";
            });
            table.AddRow([.. values]);
        }

        AnsiConsole.Write(table);
    }

    private static void WriteObjectAsTable<T>(T data)
    {
        var type = typeof(T);
        var table = new Table().Border(TableBorder.Rounded).Title($"[bold]{type.Name}[/]");
        table.AddColumn("[bold]Property[/]");
        table.AddColumn("[bold]Value[/]");

        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(data);
            table.AddRow(prop.Name, value?.ToString() ?? "");
        }

        AnsiConsole.Write(table);
    }

    public static void Success(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {EscapeMarkup(message)}");
    }

    public static void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {EscapeMarkup(message)}");
    }

    public static void Warning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠[/] {EscapeMarkup(message)}");
    }

    public static void Info(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {EscapeMarkup(message)}");
    }

    private static string EscapeMarkup(string text) =>
        text.Replace("[", "\\[", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);
}

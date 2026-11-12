using System.Text.Json;
using System.Text.Json.Nodes;
using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Converts <see cref="AgentTool"/> definitions from <see cref="IToolOrchestrator"/>
/// into <see cref="AIFunction"/> instances that can be passed to the LLM via ChatOptions.Tools.
/// </summary>
internal sealed class ToolFunctionAdapter(IToolOrchestrator orchestrator)
{
    /// <summary>
    /// Converts all registered tools into AIFunction instances.
    /// </summary>
    public IReadOnlyList<AIFunction> GetToolFunctions()
    {
        return orchestrator.GetAvailableTools()
            .Select(ConvertToFunction)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Converts a single <see cref="AgentTool"/> into an <see cref="AIFunction"/>.
    /// </summary>
    public AIFunction ConvertToFunction(AgentTool tool)
    {
        var schema = BuildParameterSchema(tool);
        return new ToolBackedAIFunction(orchestrator, tool, schema);
    }

    /// <summary>
    /// Builds a JSON Schema object from the tool's parameter dictionary.
    /// Format: { "type": "object", "properties": { ... }, "required": [...] }
    /// </summary>
    private static JsonObject BuildParameterSchema(AgentTool tool)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
        };

        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var param in tool.Parameters)
        {
            var paramName = param.Key;
            var paramDesc = param.Value;

            var propSchema = new JsonObject
            {
                ["type"] = InferJsonSchemaType(paramDesc),
                ["description"] = CleanDescription(paramDesc),
            };

            properties[paramName] = propSchema;

            if (paramDesc.Contains("required", StringComparison.OrdinalIgnoreCase))
            {
                required.Add(paramName);
            }
        }

        schema["properties"] = properties;
        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    /// <summary>
    /// Infers the JSON Schema type from a parameter description string.
    /// </summary>
    private static string InferJsonSchemaType(string description)
    {
        var lower = description.ToLowerInvariant();
        if (lower.StartsWith("integer") || lower.StartsWith("int"))
            return "integer";
        if (lower.StartsWith("number") || lower.StartsWith("decimal") || lower.StartsWith("float") || lower.StartsWith("double"))
            return "number";
        if (lower.StartsWith("boolean") || lower.StartsWith("bool"))
            return "boolean";
        if (lower.StartsWith("array") || lower.StartsWith("list"))
            return "array";

        return "string";
    }

    /// <summary>
    /// Strips the type prefix from the description to get the human-readable part.
    /// </summary>
    private static string CleanDescription(string description)
    {
        var dashIndex = description.IndexOf(" - ", StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            return description[(dashIndex + 3)..].Trim();
        }

        return description;
    }
}

/// <summary>
/// An AIFunction that delegates to the ToolOrchestrator for execution.
/// Bypasses AIFunctionFactory to avoid JsonSerializerOptions resolver issues.
/// </summary>
internal sealed class ToolBackedAIFunction(IToolOrchestrator orchestrator, AgentTool tool, JsonObject schema)
    : AIFunction
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public override string Name => tool.Name;
    public override string Description => tool.Description;
    public override JsonElement JsonSchema => JsonSerializer.SerializeToElement(schema, s_serializerOptions);

    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken = default)
    {
        var argsJson = JsonSerializer.Serialize(arguments, s_serializerOptions);
        var result = await orchestrator.ExecuteAsync(tool.Name, argsJson, cancellationToken).ConfigureAwait(false);

        // Try to deserialize as a meaningful object, fall back to string
        try
        {
            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.Clone();
        }
        catch
        {
            return result;
        }
    }
}

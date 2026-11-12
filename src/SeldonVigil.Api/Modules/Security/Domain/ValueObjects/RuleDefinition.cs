using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SeldonVigil.Api.Modules.Observability.Domain.Entities;

namespace SeldonVigil.Api.Modules.Security.Domain.ValueObjects;

public sealed record RuleDefinition
{
    public RuleType Type { get; init; }
    public string Expression { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public bool Evaluate(Trace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        return Type switch
        {
            RuleType.RegexMatch => EvaluateRegex(trace),
            RuleType.Threshold => EvaluateThreshold(trace),
            RuleType.Behavioral => EvaluateBehavioral(trace),
            RuleType.Composite => EvaluateComposite(trace),
            _ => false
        };
    }

    /// <summary>
    /// Resolves the field value a rule is evaluated against: well-known trace fields first,
    /// then trace attributes. Unknown fields yield an empty string.
    /// </summary>
    private string ResolveFieldValue(Trace trace, string field) => field switch
    {
        "operation_name" => trace.OperationName,
        "status_message" => trace.StatusMessage ?? string.Empty,
        _ => trace.Attributes.GetValueOrDefault(field, string.Empty)
    };

    private bool EvaluateRegex(Trace trace)
    {
        var pattern = GetParameter<string>("pattern");
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var field = GetParameter<string>("field") ?? "operation_name";

        try
        {
            return GetOrCompileRegex(pattern).IsMatch(ResolveFieldValue(trace, field));
        }
        catch (ArgumentException)
        {
            // Invalid pattern expressions must not crash the analysis pipeline.
            return false;
        }
    }

    private bool EvaluateThreshold(Trace trace)
    {
        var metric = GetParameter<string>("metric");
        var op = GetParameter<string>("operator");
        var threshold = GetParameter<double?>("value");

        if (string.IsNullOrEmpty(metric) || string.IsNullOrEmpty(op) || threshold is null)
        {
            return false;
        }

        var actualValue = ResolveMetric(trace, metric);
        if (actualValue is null)
        {
            return false;
        }

        return op switch
        {
            "gt" or "greater_than" => actualValue > threshold,
            "gte" or "greater_than_or_equal" => actualValue >= threshold,
            "lt" or "less_than" => actualValue < threshold,
            "lte" or "less_than_or_equal" => actualValue <= threshold,
            "eq" or "equal" => Math.Abs(actualValue.Value - threshold.Value) < 0.0001,
            _ => false
        };
    }

    /// <summary>
    /// Flags statistically anomalous metric values against baseline parameters:
    /// {metric, mean, sigma, max_z}. A value violates when its z-score
    /// |value - mean| / sigma reaches max_z. With sigma &lt;= 0 an absolute-deviation
    /// comparison against mean + max_deviation is used instead.
    /// </summary>
    private bool EvaluateBehavioral(Trace trace)
    {
        var metric = GetParameter<string>("metric");
        var mean = GetParameter<double?>("mean");
        var maxZ = GetParameter<double?>("max_z") ?? 3.0;

        if (string.IsNullOrEmpty(metric) || mean is null)
        {
            return false;
        }

        var actualValue = ResolveMetric(trace, metric);
        if (actualValue is null)
        {
            return false;
        }

        var sigma = GetParameter<double?>("sigma") ?? 0.0;
        if (sigma > 0)
        {
            return Math.Abs(actualValue.Value - mean.Value) / sigma >= maxZ;
        }

        var maxDeviation = GetParameter<double?>("max_deviation") ?? 0.0;
        return actualValue.Value >= mean.Value + maxDeviation;
    }

    /// <summary>
    /// Combines sub-rules from Parameters["rules"] using Expression as the combiner:
    /// "all"/"and", "any"/"or", or "not". Sub-rules may themselves be composite.
    /// </summary>
    private bool EvaluateComposite(Trace trace)
    {
        var subRules = GetSubRules();
        if (subRules.Count == 0)
        {
            return false;
        }

        return Expression.Trim().ToLowerInvariant() switch
        {
            "all" or "and" => subRules.All(r => r.Evaluate(trace)),
            "any" or "or" => subRules.Any(r => r.Evaluate(trace)),
            "not" => !subRules[0].Evaluate(trace),
            _ => false
        };
    }

    private List<RuleDefinition> GetSubRules()
    {
        if (!Parameters.TryGetValue("rules", out var rulesObj))
        {
            return [];
        }

        return rulesObj switch
        {
            IEnumerable<RuleDefinition> typed => [.. typed],
            JsonElement { ValueKind: JsonValueKind.Array } array =>
                [.. array.EnumerateArray().Select(ToRuleDefinition)],
            _ => []
        };
    }

    // Sub-rules arrive as arbitrary JSON; string enum names require an explicit
    // JsonStringEnumConverter because .NET 10 only maps enum names when configured.
    private static readonly JsonSerializerOptions SubRuleSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static RuleDefinition ToRuleDefinition(JsonElement element)
    {
        try
        {
            return JsonSerializer.Deserialize<RuleDefinition>(element.GetRawText(), SubRuleSerializerOptions)
                ?? new RuleDefinition { Type = RuleType.Composite, Expression = "invalid" };
        }
        catch (JsonException)
        {
            return new RuleDefinition { Type = RuleType.Composite, Expression = "invalid" };
        }
    }

    /// <summary>
    /// Numeric metrics available to threshold/behavioral rules. Known metrics map to
    /// trace properties; anything else falls back to a numeric trace attribute.
    /// Returns null when the metric is not present on this trace.
    /// </summary>
    private static double? ResolveMetric(Trace trace, string metric) => metric switch
    {
        "duration_ms" => trace.Duration.TotalMilliseconds,
        "token_usage_total" => trace.TokenUsage.TotalTokens,
        "cost_usd" => (double)trace.Cost.Amount,
        _ when double.TryParse(
                trace.Attributes.GetValueOrDefault(metric, string.Empty),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed) => parsed,
        _ => null
    };

    private T? GetParameter<T>(string name)
    {
        if (!Parameters.TryGetValue(name, out var raw))
        {
            return default;
        }

        return Coerce<T>(raw);
    }

    /// <summary>
    /// Parameters arrive either as native CLR values (rules built in code) or as
    /// System.Text.Json.JsonElement instances (rules deserialized from the jsonb
    /// column by RuleDefinitionConverter). Coerce both to the target type.
    /// </summary>
    internal static T? Coerce<T>(object? raw)
    {
        if (raw is null)
        {
            return default;
        }

        if (raw is JsonElement element)
        {
            return CoerceJsonElement<T>(element);
        }

        if (raw is T typed)
        {
            return typed;
        }

        if (raw is IConvertible convertible && typeof(T) != typeof(string))
        {
            try
            {
                return (T?)Convert.ChangeType(convertible, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException or FormatException)
            {
                return default;
            }
        }

        return default;
    }

    private static T? CoerceJsonElement<T>(JsonElement element)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType == typeof(string))
        {
            return (T)(object)(element.ValueKind == JsonValueKind.String
                ? element.GetString()!
                : element.GetRawText());
        }

        if (targetType == typeof(double) || targetType == typeof(double?))
        {
            return element.ValueKind is JsonValueKind.Number
                ? (T)(object)element.GetDouble()
                : double.TryParse(element.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    ? (T)(object)d
                    : default;
        }

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            return element.ValueKind is JsonValueKind.Number
                ? (T)(object)element.GetInt32()
                : int.TryParse(element.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
                    ? (T)(object)i
                    : default;
        }

        return default;
    }

    private static Regex GetOrCompileRegex(string pattern) =>
        RegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));
}

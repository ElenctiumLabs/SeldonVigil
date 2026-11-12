namespace SeldonVigil.Api.Modules.Observability.Application;

internal static class TraceThreatScreen
{
    // Known direct prompt-injection vectors screened at ingestion time so
    // obviously hostile payloads are rejected synchronously (defence in depth
    // alongside the asynchronous rule evaluation performed downstream by the
    // SecurityAnalyzerWorker).
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous",
        "ignore all previous",
        "ignore above",
        "disregard previous",
        "disregard all previous",
        "dan mode",
        "developer mode",
        "jailbreak",
        "bypass all security",
        "bypass security checks",
        "reveal your system prompt",
        "reveal system prompt"
    ];

    public static bool IsPromptInjection(string? operationName, IReadOnlyDictionary<string, string> attributes)
    {
        if (!string.IsNullOrWhiteSpace(operationName) &&
            ContainsPattern(operationName))
        {
            return true;
        }

        foreach (var value in attributes.Values)
        {
            if (value is string s && ContainsPattern(s))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsPattern(string input)
    {
        return InjectionPatterns.Any(pattern => input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

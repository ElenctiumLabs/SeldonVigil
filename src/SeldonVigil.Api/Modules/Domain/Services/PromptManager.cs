using System.Text.RegularExpressions;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.SharedKernel.Infrastructure;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// Manages prompt templates with Git-backed versioning, A/B testing,
/// and dynamic variable substitution. Prevents prompt injection in templates.
/// </summary>
internal sealed partial class PromptManager(
    IPromptRepository repository,
    ICacheService cache,
    ILogger<PromptManager> logger)
    : IPromptManager
{
    private readonly FluidParser _fluidParser = new();

    [LoggerMessage(Level = LogLevel.Information, Message = "Created prompt version {PromptName} v{Version}")]
    private static partial void LogPromptVersionCreated(ILogger logger, string promptName, int version);

    [LoggerMessage(Level = LogLevel.Information, Message = "Activated prompt {PromptName} v{Version}")]
    private static partial void LogPromptVersionActivated(ILogger logger, string promptName, int version);

    public async Task<PromptAssembly> GetPromptAsync(
        string promptName,
        Dictionary<string, object> variables,
        CancellationToken ct = default)
    {
        var cacheKey = $"seldonvigil:prompt:{promptName}";
        var cached = await cache.GetAsync<PromptVersion>(cacheKey, ct).ConfigureAwait(false);

        PromptVersion prompt;
        if (cached is not null)
        {
            prompt = cached;
        }
        else
        {
            prompt = await repository.GetActiveVersionAsync(promptName, ct).ConfigureAwait(false)
                ?? throw new PromptNotFoundException($"Prompt '{promptName}' not found.");

            await cache.SetAsync(cacheKey, prompt, TimeSpan.FromMinutes(5), ct)
                .ConfigureAwait(false);
        }

        // Sanitize variables to prevent prompt injection
        var sanitizedVariables = SanitizeVariables(variables);

        // Assemble prompt with Fluid templating
        var template = _fluidParser.Parse(prompt.Template);
        var context = new TemplateContext(sanitizedVariables);

        var assembled = await template.RenderAsync(context).ConfigureAwait(false);

        // Validate assembled prompt
        ValidateAssembledPrompt(assembled);

        return new PromptAssembly(
            prompt.Name,
            prompt.Version,
            assembled,
            prompt.SystemPrompt,
            prompt.ModelConfig);
    }

    public async Task<PromptVersion> CreateVersionAsync(
        string promptName,
        string template,
        string systemPrompt,
        ModelConfiguration config,
        Guid createdBy,
        CancellationToken ct = default)
    {
        // Validate template syntax
        if (!_fluidParser.TryParse(template, out var _, out var error))
        {
            throw new PromptValidationException($"Invalid template syntax: {error}");
        }

        // Check for potential prompt injection vectors in template
        ValidateTemplateSecurity(template);

        var version = await repository.GetLatestVersionAsync(promptName, ct).ConfigureAwait(false);
        var newVersion = new PromptVersion
        {
            Name = promptName,
            Version = (version?.Version ?? 0) + 1,
            Template = template,
            SystemPrompt = systemPrompt,
            ModelConfig = config,
            Status = PromptStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        await repository.AddAsync(newVersion, ct).ConfigureAwait(false);

        LogPromptVersionCreated(logger, promptName, newVersion.Version);

        return newVersion;
    }

    public async Task ActivateVersionAsync(
        string promptName,
        int version,
        CancellationToken ct = default)
    {
        await repository.ActivateVersionAsync(promptName, version, ct).ConfigureAwait(false);
        await cache.RemoveAsync($"seldonvigil:prompt:{promptName}", ct).ConfigureAwait(false);

        LogPromptVersionActivated(logger, promptName, version);
    }

    private static Dictionary<string, object> SanitizeVariables(Dictionary<string, object> variables)
    {
        var sanitized = new Dictionary<string, object>();
        foreach (var (key, value) in variables)
        {
            if (value is string str)
            {
                // Escape template delimiters to prevent injection
                sanitized[key] = str
                    .Replace("{{", "\\{\\{")
                    .Replace("}}", "\\}\\}")
                    .Replace("{%", "\\{%")
                    .Replace("%}", "\\%}");
            }
            else
            {
                sanitized[key] = value;
            }
        }
        return sanitized;
    }

    private static void ValidateTemplateSecurity(string template)
    {
        // Prevent dangerous template constructs
        var dangerousPatterns = new[]
        {
            @"\{\%\s*assign\s+.*\=\s*.*\%\}", // Variable assignment
            @"\{\%\s*include\s+.*\%\}", // File inclusion
            @"\{\%\s*raw\s*\%\}", // Raw blocks that bypass escaping
        };

        var dangerousPattern = dangerousPatterns.FirstOrDefault(
            pattern => Regex.IsMatch(template, pattern, RegexOptions.IgnoreCase));
        if (dangerousPattern is not null)
        {
            throw new PromptValidationException($"Template contains potentially dangerous construct: {dangerousPattern}");
        }
    }

    private static void ValidateAssembledPrompt(string prompt)
    {
        // Check for common prompt injection patterns in assembled output
        var injectionPatterns = new[]
        {
            @"ignore\s+(previous|above|all)\s+instructions",
            @"forget\s+(everything|all|your)\s+(instructions|training)",
            @"system\s*:\s*you\s+are\s+now",
            @"DAN\s*\(",
        };

        if (injectionPatterns.Any(pattern => Regex.IsMatch(prompt, pattern, RegexOptions.IgnoreCase)))
        {
            throw new PromptValidationException("Assembled prompt contains potential injection patterns.");
        }
    }
}

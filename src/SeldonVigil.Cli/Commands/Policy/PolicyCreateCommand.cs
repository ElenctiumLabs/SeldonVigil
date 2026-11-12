using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Extensions;
using SeldonVigil.Sdk.Security;

namespace SeldonVigil.Cli.Commands.Policy;

internal static class PolicyCreateCommand
{
    public static Command Create()
    {
        var nameOption = new Option<string>("--name")
        {
            Description = "Policy name",
        };
        var ruleOption = new Option<string>("--rule")
        {
            Description = "Rule definition (JSON or YAML)",
        };
        var descriptionOption = new Option<string?>("--description")
        {
            Description = "Policy description",
        };
        var severityOption = new Option<string?>("--severity")
        {
            Description = "Severity level (Low, Medium, High, Critical)",
        };
        var categoryOption = new Option<string?>("--category")
        {
            Description = "Policy category",
        };

        var command = new Command("create", "Create a new security policy")
        {
            nameOption,
            ruleOption,
            descriptionOption,
            severityOption,
            categoryOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var rule = parseResult.GetValue(ruleOption)!;
            var description = parseResult.GetValue(descriptionOption);
            var severity = parseResult.GetValue(severityOption);
            var category = parseResult.GetValue(categoryOption);
            return await ExecuteAsync(name, rule, description, severity, category).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string name, string rule, string? description, string? severity, string? category)
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
            var policyId = await securityClient.CreatePolicyAsync(new CreateSecurityPolicyRequest
            {
                Name = name,
                Description = description,
                RuleDefinition = rule,
                Severity = severity,
                Category = category,
            }).ConfigureAwait(false);

            OutputFormatter.Success($"Policy '{name}' created with ID: {policyId}");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to create policy: {ex.Message}");
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

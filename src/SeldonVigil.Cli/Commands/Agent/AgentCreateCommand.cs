using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Agents;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Agent;

internal static class AgentCreateCommand
{
    public static Command Create()
    {
        var nameOption = new Option<string>("--name")
        {
            Description = "Agent name",
        };
        var frameworkOption = new Option<string>("--framework")
        {
            Description = "Agent framework (LangChain, CrewAI, etc.)",
            DefaultValueFactory = _ => "Custom",
        };
        var modelProviderOption = new Option<string>("--model-provider")
        {
            Description = "Model provider (OpenAI, Anthropic, etc.)",
            DefaultValueFactory = _ => "Custom",
        };
        var modelNameOption = new Option<string>("--model-name")
        {
            Description = "Model name",
            DefaultValueFactory = _ => string.Empty,
        };
        var externalIdOption = new Option<string?>("--external-id")
        {
            Description = "External ID",
        };
        var descriptionOption = new Option<string?>("--description")
        {
            Description = "Agent description",
        };

        var command = new Command("create", "Register a new agent")
        {
            nameOption,
            frameworkOption,
            modelProviderOption,
            modelNameOption,
            externalIdOption,
            descriptionOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var framework = parseResult.GetValue(frameworkOption)!;
            var modelProvider = parseResult.GetValue(modelProviderOption)!;
            var modelName = parseResult.GetValue(modelNameOption)!;
            var externalId = parseResult.GetValue(externalIdOption);
            var description = parseResult.GetValue(descriptionOption);
            return await ExecuteAsync(name, framework, modelProvider, modelName, externalId, description).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(
        string name, string framework, string modelProvider, string modelName,
        string? externalId, string? description)
    {
        var config = ConfigManager.Load();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
        {
            OutputFormatter.Error("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var services = BuildServiceProvider(config);
        var agentClient = services.GetRequiredService<AgentClient>();

        try
        {
            var request = new CreateAgentRequest
            {
                Name = name,
                Framework = framework,
                ModelProvider = modelProvider,
                ModelName = modelName,
                ExternalId = externalId ?? string.Empty,
                Description = description,
            };

            var agent = await agentClient.RegisterAgentAsync(request).ConfigureAwait(false);
            OutputFormatter.Success($"Agent created: {agent.Id}");
            OutputFormatter.Write(agent, "json");
            return 0;
        }
        catch (Exception ex)
        {
            OutputFormatter.Error($"Failed to create agent: {ex.Message}");
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

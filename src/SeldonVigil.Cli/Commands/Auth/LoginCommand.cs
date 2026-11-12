using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SeldonVigil.Cli.Infrastructure;
using SeldonVigil.Sdk.Authentication;
using SeldonVigil.Sdk.Extensions;

namespace SeldonVigil.Cli.Commands.Auth;

internal static class LoginCommand
{
    public static Command Create()
    {
        var apiKeyOption = new Option<string?>("--api-key")
        {
            Description = "API key for authentication",
        };
        var baseUrlOption = new Option<string?>("--url")
        {
            Description = "SeldonVigil API base URL",
        };

        var command = new Command("login", "Authenticate with the SeldonVigil API")
        {
            apiKeyOption,
            baseUrlOption,
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var apiKey = parseResult.GetValue(apiKeyOption);
            var baseUrl = parseResult.GetValue(baseUrlOption);
            return await ExecuteAsync(apiKey, baseUrl).ConfigureAwait(false);
        });
        return command;
    }

    private static async Task<int> ExecuteAsync(string? apiKey, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            OutputFormatter.Error("API key is required. Use: seldonvigil login --api-key YOUR_KEY");
            return 1;
        }

        var config = ConfigManager.Load();
        config.ApiKey = apiKey;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            config.BaseUrl = baseUrl;
        }

        var services = BuildServiceProvider(config);
        var authClient = services.GetRequiredService<SeldonVigilAuthenticationClient>();

        try
        {
            var token = await authClient.LoginWithPasswordAsync("cli", apiKey).ConfigureAwait(false);
            config.AccessToken = token.AccessToken;
            config.RefreshToken = token.RefreshToken;
            config.TokenExpiresAt = token.ExpiresAt;
            config.Save();

            OutputFormatter.Success($"Authenticated. Token expires at {token.ExpiresAt:u}.");
            return 0;
        }
        catch (Exception)
        {
            config.Save();
            OutputFormatter.Success("API key saved. The server will authenticate using it directly.");
            return 0;
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

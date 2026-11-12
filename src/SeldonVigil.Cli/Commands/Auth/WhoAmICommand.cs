using System.CommandLine;
using SeldonVigil.Cli.Infrastructure;

namespace SeldonVigil.Cli.Commands.Auth;

internal static class WhoAmICommand
{
    public static Command Create()
    {
        var command = new Command("whoami", "Display current authentication session");
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ExecuteAsync().ConfigureAwait(false);
        });
        return command;
    }

    private static Task<int> ExecuteAsync()
    {
        var config = ConfigManager.Load();

        if (string.IsNullOrWhiteSpace(config.ApiKey) && string.IsNullOrWhiteSpace(config.AccessToken))
        {
            OutputFormatter.Warning("Not authenticated. Run: seldonvigil login --api-key YOUR_KEY");
            return Task.FromResult(1);
        }

        OutputFormatter.Success("Authenticated");

        var data = new
        {
            baseUrl = config.BaseUrl ?? "https://localhost:5001",
            hasApiKey = !string.IsNullOrWhiteSpace(config.ApiKey),
            hasToken = !string.IsNullOrWhiteSpace(config.AccessToken),
            tokenExpiresAt = config.TokenExpiresAt?.ToString("u") ?? "N/A",
        };

        OutputFormatter.Write(data, "json");
        return Task.FromResult(0);
    }
}

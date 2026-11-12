using System.CommandLine;
using SeldonVigil.Cli.Infrastructure;

namespace SeldonVigil.Cli.Commands.Auth;

internal static class LogoutCommand
{
    public static Command Create()
    {
        var command = new Command("logout", "Clear stored authentication credentials");
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            return await ExecuteAsync().ConfigureAwait(false);
        });
        return command;
    }

    private static Task<int> ExecuteAsync()
    {
        var config = ConfigManager.Load();
        config.Clear();
        OutputFormatter.Success("Logged out. Credentials cleared.");
        return Task.FromResult(0);
    }
}

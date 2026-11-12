using System.CommandLine;
using SeldonVigil.Cli.Commands.Agent;
using SeldonVigil.Cli.Commands.Auth;
using SeldonVigil.Cli.Commands.Budget;
using SeldonVigil.Cli.Commands.Policy;
using SeldonVigil.Cli.Commands.Seed;
using SeldonVigil.Cli.Commands.Trace;

namespace SeldonVigil.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        var rootCommand = new RootCommand("SeldonVigil CLI - AI Agent Governance Platform")
        {
            CreateAuthCommand(),
            CreateAgentCommand(),
            CreatePolicyCommand(),
            CreateTraceCommand(),
            CreateBudgetCommand(),
            SeedCommand.Create(),
        };

        return rootCommand.Parse(args).Invoke();
    }

    private static Command CreateAuthCommand()
    {
        var authCommand = new Command("auth", "Authentication commands")
        {
            LoginCommand.Create(),
            WhoAmICommand.Create(),
            LogoutCommand.Create(),
        };
        return authCommand;
    }

    private static Command CreateAgentCommand()
    {
        var agentCommand = new Command("agent", "Manage registered AI agents")
        {
            AgentListCommand.Create(),
            AgentGetCommand.Create(),
            AgentCreateCommand.Create(),
            AgentSuspendCommand.Create(),
            AgentActivateCommand.Create(),
            AgentDeleteCommand.Create(),
        };
        return agentCommand;
    }

    private static Command CreatePolicyCommand()
    {
        var policyCommand = new Command("policy", "Manage security policies")
        {
            PolicyListCommand.Create(),
            PolicyGetCommand.Create(),
            PolicyCreateCommand.Create(),
            PolicyDisableCommand.Create(),
            PolicyEvaluateCommand.Create(),
        };
        return policyCommand;
    }

    private static Command CreateTraceCommand()
    {
        var traceCommand = new Command("trace", "Query observability traces")
        {
            TraceListCommand.Create(),
            TraceGetCommand.Create(),
            TraceExportCommand.Create(),
        };
        return traceCommand;
    }

    private static Command CreateBudgetCommand()
    {
        var budgetCommand = new Command("budget", "Manage cost budgets")
        {
            BudgetListCommand.Create(),
            BudgetCreateCommand.Create(),
            BudgetResetCommand.Create(),
        };
        return budgetCommand;
    }
}

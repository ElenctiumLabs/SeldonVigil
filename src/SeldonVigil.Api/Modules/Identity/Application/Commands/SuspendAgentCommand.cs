using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record SuspendAgentCommand(AgentId AgentId, string Reason) : ICommand;

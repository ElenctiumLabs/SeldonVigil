using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed record GetAgentByIdQuery(AgentId AgentId) : IQuery<AgentDto>;

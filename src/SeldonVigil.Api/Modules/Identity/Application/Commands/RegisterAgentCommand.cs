using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Application;

namespace SeldonVigil.Api.Modules.Identity.Application.Commands;

internal sealed record RegisterAgentCommand(
    string ExternalId,
    string Name,
    string? Description,
    AgentFramework Framework,
    string? FrameworkVersion,
    ModelProvider ModelProvider,
    string ModelName,
    string? ModelVersion,
    CapabilityManifest Manifest
) : ICommand<AgentId>;

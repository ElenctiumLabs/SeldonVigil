using SeldonVigil.Api.Modules.Identity.Domain.Enums;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Api.Endpoints;

internal sealed record RegisterAgentRequest(
    string ExternalId,
    string Name,
    string? Description,
    AgentFramework Framework,
    string? FrameworkVersion,
    ModelProvider ModelProvider,
    string ModelName,
    string? ModelVersion,
    CapabilityManifest Manifest);

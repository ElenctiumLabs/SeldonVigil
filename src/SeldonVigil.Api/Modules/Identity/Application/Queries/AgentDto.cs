using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Application.Queries;

internal sealed record AgentDto(
    AgentId Id,
    string ExternalId,
    string Name,
    string? Description,
    string Framework,
    string? FrameworkVersion,
    string ModelProvider,
    string ModelName,
    string? ModelVersion,
    string Status,
    CapabilityManifest CapabilityManifest,
    Guid? IdentityTokenJti,
    DateTime? IdentityTokenExpiresAt,
    int SourceVersion,
    string? SourceCodeHash,
    string? PromptTemplateHash,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ActivatedAt,
    DateTime? SuspendedAt,
    string? SuspendedReason,
    DateTime? RevokedAt,
    string? RevokedReason);

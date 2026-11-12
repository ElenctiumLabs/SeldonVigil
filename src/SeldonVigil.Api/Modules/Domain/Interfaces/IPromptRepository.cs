using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IPromptRepository
{
    Task<PromptVersion?> GetActiveVersionAsync(string promptName, CancellationToken ct = default);
    Task<PromptVersion?> GetLatestVersionAsync(string promptName, CancellationToken ct = default);
    Task AddAsync(PromptVersion version, CancellationToken ct = default);
    Task ActivateVersionAsync(string promptName, int version, CancellationToken ct = default);
}

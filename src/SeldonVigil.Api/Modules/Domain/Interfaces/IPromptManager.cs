using SeldonVigil.Api.Modules.Domain.Services;

namespace SeldonVigil.Api.Modules.Domain.Interfaces;

internal interface IPromptManager
{
    Task<PromptAssembly> GetPromptAsync(string promptName, Dictionary<string, object> variables, CancellationToken ct = default);
    Task<PromptVersion> CreateVersionAsync(string promptName, string template, string systemPrompt, ModelConfiguration config, Guid createdBy, CancellationToken ct = default);
    Task ActivateVersionAsync(string promptName, int version, CancellationToken ct = default);
}

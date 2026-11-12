namespace SeldonVigil.Api.Modules.Domain.Services;

internal sealed record PromptAssembly(
    string Name,
    int Version,
    string UserPrompt,
    string SystemPrompt,
    ModelConfiguration ModelConfig);

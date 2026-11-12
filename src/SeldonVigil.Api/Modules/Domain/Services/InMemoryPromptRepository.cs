using SeldonVigil.Api.Modules.Domain.Interfaces;

namespace SeldonVigil.Api.Modules.Domain.Services;

/// <summary>
/// In-memory prompt repository for development. Stores prompt versions in memory.
/// </summary>
internal sealed class InMemoryPromptRepository : IPromptRepository
{
    private readonly Dictionary<string, List<PromptVersion>> _prompts = new();

    public Task<PromptVersion?> GetActiveVersionAsync(string promptName, CancellationToken ct = default)
    {
        if (_prompts.TryGetValue(promptName, out var versions))
        {
            var active = versions
                .Where(v => v.Status == PromptStatus.Active)
                .OrderByDescending(v => v.Version)
                .FirstOrDefault();
            return Task.FromResult(active);
        }

        return Task.FromResult<PromptVersion?>(null);
    }

    public Task<PromptVersion?> GetLatestVersionAsync(string promptName, CancellationToken ct = default)
    {
        if (_prompts.TryGetValue(promptName, out var versions))
        {
            return Task.FromResult(versions.OrderByDescending(v => v.Version).FirstOrDefault());
        }

        return Task.FromResult<PromptVersion?>(null);
    }

    public Task AddAsync(PromptVersion version, CancellationToken ct = default)
    {
        if (!_prompts.ContainsKey(version.Name))
        {
            _prompts[version.Name] = new List<PromptVersion>();
        }

        _prompts[version.Name].Add(version);
        return Task.CompletedTask;
    }

    public Task ActivateVersionAsync(string promptName, int version, CancellationToken ct = default)
    {
        if (_prompts.TryGetValue(promptName, out var versions))
        {
            foreach (var v in versions.Where(v => v.Version == version))
            {
                // Since PromptVersion is an immutable record, we can't modify it directly.
                // In a real implementation, this would update the database.
            }
        }

        return Task.CompletedTask;
    }
}

using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence.Entities;
using SeldonVigil.Api.Modules.Domain.Interfaces;
using SeldonVigil.Api.Modules.Domain.Services;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for prompt versions (replaces the in-memory store so
/// prompts survive restarts).
/// </summary>
internal sealed class PostgresPromptRepository(SeldonVigilDbContext db) : IPromptRepository
{
    public async Task<PromptVersion?> GetActiveVersionAsync(string promptName, CancellationToken ct = default)
    {
        var entity = await db.PromptVersions
            .Where(v => v.Name == promptName && v.Status == PromptStatus.Active)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return ToDomain(entity);
    }

    public async Task<PromptVersion?> GetLatestVersionAsync(string promptName, CancellationToken ct = default)
    {
        var entity = await db.PromptVersions
            .Where(v => v.Name == promptName)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return ToDomain(entity);
    }

    public async Task AddAsync(PromptVersion version, CancellationToken ct = default)
    {
        var entity = ToEntity(version);
        var tracked = await db.PromptVersions.FindAsync([entity.Id], ct).ConfigureAwait(false);

        if (tracked is null)
        {
            db.PromptVersions.Add(entity);
        }
        else
        {
            db.Entry(tracked).CurrentValues.SetValues(entity);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ActivateVersionAsync(string promptName, int version, CancellationToken ct = default)
    {
        var versions = await db.PromptVersions
            .Where(v => v.Name == promptName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var v in versions)
        {
            if (v.Version == version)
            {
                v.Status = PromptStatus.Active;
            }
            else if (v.Status == PromptStatus.Active)
            {
                v.Status = PromptStatus.Deprecated;
            }

            v.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static PromptVersionEntity ToEntity(PromptVersion version) => new()
    {
        Id = Guid.NewGuid(),
        Name = version.Name,
        Version = version.Version,
        Template = version.Template,
        SystemPrompt = version.SystemPrompt,
        ModelConfig = version.ModelConfig,
        Status = version.Status,
        CreatedBy = version.CreatedBy,
        CreatedAt = version.CreatedAt,
        UpdatedAt = DateTime.UtcNow
    };

    private static PromptVersion? ToDomain(PromptVersionEntity? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new PromptVersion
        {
            Name = entity.Name,
            Version = entity.Version,
            Template = entity.Template,
            SystemPrompt = entity.SystemPrompt,
            ModelConfig = entity.ModelConfig,
            Status = entity.Status,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt
        };
    }
}

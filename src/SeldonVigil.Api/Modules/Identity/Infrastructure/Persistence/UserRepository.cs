using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserRepository(
    SeldonVigilDbContext dbContext)
    : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(TenantId tenantId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.TenantId == tenantId && u.Email == normalizedEmail,
                ct);
    }

    public Task<bool> ExistsAsync(TenantId tenantId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.TenantId == tenantId && u.Email == normalizedEmail,
                ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await dbContext.Users.AddAsync(user, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

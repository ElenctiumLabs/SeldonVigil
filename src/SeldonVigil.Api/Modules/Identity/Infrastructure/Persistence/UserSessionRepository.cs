using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Repositories;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;

internal sealed class UserSessionRepository(
    SeldonVigilDbContext dbContext)
    : IUserSessionRepository
{
    public Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
        => dbContext.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, ct);

    public async Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        await dbContext.UserSessions.AddAsync(session, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserSession session, CancellationToken ct = default)
    {
        dbContext.UserSessions.Update(session);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string reason, DateTime revokedAt, CancellationToken ct = default)
    {
        var sessions = await dbContext.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var session in sessions)
        {
            session.Revoke(reason, revokedAt);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

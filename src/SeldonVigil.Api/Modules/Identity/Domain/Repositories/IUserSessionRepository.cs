using SeldonVigil.Api.Modules.Identity.Domain.Entities;

namespace SeldonVigil.Api.Modules.Identity.Domain.Repositories;

internal interface IUserSessionRepository
{
    Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default);
    Task AddAsync(UserSession session, CancellationToken ct = default);
    Task UpdateAsync(UserSession session, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, string reason, DateTime revokedAt, CancellationToken ct = default);
}

using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Analytics.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Persistence;

namespace SeldonVigil.Api.Modules.Analytics.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for notifications (replaces the in-memory store so
/// notification history survives restarts). Soft-delete aware.
/// </summary>
internal sealed class NotificationRepository(SeldonVigilDbContext db)
{
    public async Task<IReadOnlyList<Notification>> GetAllAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var query = db.Notifications.Where(n => !n.IsDeleted);
        if (unreadOnly)
        {
            query = query.Where(n => !n.Read);
        }

        var results = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return results.AsReadOnly();
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.Notifications.FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

    public async Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Persists mutations made on a loaded entity.</summary>
    public async Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        notification.MarkAsUpdated();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

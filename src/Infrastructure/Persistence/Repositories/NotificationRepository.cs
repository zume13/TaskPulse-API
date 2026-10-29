using Domain.Aggregates.Notifications;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public sealed class NotificationRepository
    : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(TaskPulseDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <summary>
    /// Retrieves a recipient's notifications, newest first.
    /// </summary>
    public async Task<List<Notification>> GetByRecipientAsync(
        Guid recipientId,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext
            .Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == recipientId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);
        

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .ThenByDescending(n => n.Id)
            .Skip(Math.Max(0, skip))
            .Take(Math.Max(0, take))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Counts unread notifications for a recipient.
    /// </summary>
    public async Task<int> CountUnreadAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Notifications
            .AsNoTracking()
            .CountAsync(
                n => n.RecipientId == recipientId && !n.IsRead,
                cancellationToken);
    }

    /// <summary>
    /// Marks all unread notifications belonging to a recipient as read.
    /// Executes immediately and bypasses the Unit of Work.
    /// </summary>
    public async Task<int> MarkAllAsReadAsync(
        Guid recipientId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, (DateTime?)nowUtc),
                cancellationToken);
    }
}
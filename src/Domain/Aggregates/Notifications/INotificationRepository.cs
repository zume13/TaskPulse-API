using Domain.Shared.Base;

namespace Domain.Aggregates.Notifications
{
    public interface INotificationRepository : IRepository<Notification>
    {
        /// <summary>Newest first.</summary>
        Task<List<Notification>> GetByRecipientAsync(
            Guid recipientId,
            bool unreadOnly,
            int skip,
            int take,
            CancellationToken cancellationToken = default);

        Task<int> CountUnreadAsync(
            Guid recipientId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Set-based update (runs immediately, does not go through the
        /// UnitOfWork). Returns how many notifications were marked.
        /// </summary>
        Task<int> MarkAllAsReadAsync(
            Guid recipientId,
            DateTime nowUtc,
            CancellationToken cancellationToken = default);
    }
}

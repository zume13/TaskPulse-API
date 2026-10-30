using Domain.Aggregates.Notifications;
using Domain.Aggregates.Notifications.Enums;

namespace Application.Features.Notifications;

internal static class NotificationHelper
{
    /// <summary>
    /// Creates a notification and adds it to the repository. It is not saved
    /// here: the outbox job saves it together with the handler's consumer row.
    /// </summary>
    public static async Task AddAsync(
        INotificationRepository notificationRepository,
        TimeProvider timeProvider,
        Guid recipientId,
        NotificationType type,
        string title,
        string message,
        Guid? workspaceId,
        NotificationTarget targetType,
        Guid? targetId,
        CancellationToken cancellationToken)
    {
        var result = Notification.Create(
            recipientId,
            type,
            title,
            message,
            workspaceId,
            targetType,
            targetId,
            timeProvider.GetUtcNow().UtcDateTime);

        // Throwing lets the outbox job retry, and dead-letter, the message.
        if (result.IsFailure)
            throw new InvalidOperationException($"Could not create the {type} notification: {result.Error.code}.");

        await notificationRepository.AddAsync(result.value, cancellationToken);
    }
}

using Domain.Aggregates.Notifications.Enums;
using Domain.Aggregates.Notifications.Errors;
using Domain.Shared.Primitives;
using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Notifications
{
    /// <summary>
    /// An in-app notification for one user. Title and message are stored as
    /// rendered text (a snapshot), so later renames do not rewrite history.
    /// </summary>
    public class Notification : AggregateRoot
    {
        public const int MaxTitleLength = 150;
        public const int MaxMessageLength = 500;

        private Notification() { } // EF Core

        private Notification(
            Guid id,
            Guid recipientId,
            NotificationType type,
            string title,
            string message,
            Guid? workspaceId,
            NotificationTarget targetType,
            Guid? targetId,
            DateTime createdAtUtc)
            : base(id)
        {
            RecipientId = recipientId;
            Type = type;
            Title = title;
            Message = message;
            WorkspaceId = workspaceId;
            TargetType = targetType;
            TargetId = targetId;
            CreatedAtUtc = createdAtUtc;
        }

        public Guid RecipientId { get; private set; }

        public NotificationType Type { get; private set; }

        public string Title { get; private set; } = null!;

        public string Message { get; private set; } = null!;

        public Guid? WorkspaceId { get; private set; }

        public NotificationTarget TargetType { get; private set; }

        public Guid? TargetId { get; private set; }

        public bool IsRead { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        public DateTime? ReadAtUtc { get; private set; }

        public static ResultT<Notification> Create(
            Guid recipientId,
            NotificationType type,
            string? title,
            string? message,
            Guid? workspaceId,
            NotificationTarget targetType,
            Guid? targetId,
            DateTime nowUtc)
        {
            if (recipientId == Guid.Empty)
                return GeneralErrors.General.Empty(nameof(recipientId));

            if (!Enum.IsDefined(type))
                return NotificationErrors.InvalidType;

            if (string.IsNullOrWhiteSpace(title))
                return GeneralErrors.General.Empty(nameof(title));

            if (string.IsNullOrWhiteSpace(message))
                return GeneralErrors.General.Empty(nameof(message));

            var trimmedTitle = title.Trim();
            var trimmedMessage = message.Trim();

            if (trimmedTitle.Length > MaxTitleLength)
                return NotificationErrors.TitleTooLong;

            if (trimmedMessage.Length > MaxMessageLength)
                return NotificationErrors.MessageTooLong;

            if (!Enum.IsDefined(targetType))
                return NotificationErrors.InvalidTarget;

            if (targetType != NotificationTarget.None
                && (targetId is null || targetId == Guid.Empty))
            {
                return NotificationErrors.TargetIdRequired;
            }

            return new Notification(
                Guid.NewGuid(),
                recipientId,
                type,
                trimmedTitle,
                trimmedMessage,
                workspaceId,
                targetType,
                targetType == NotificationTarget.None ? null : targetId,
                nowUtc);
        }

        /// <summary>Marking an already-read notification is a no-op, not an error.</summary>
        public Result MarkAsRead(Guid actorId, DateTime nowUtc)
        {
            if (actorId != RecipientId)
                return NotificationErrors.NotOwnedByUser;

            if (IsRead)
                return Result.Success();

            IsRead = true;
            ReadAtUtc = nowUtc;

            return Result.Success();
        }
    }
}

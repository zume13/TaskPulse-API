using Domain.Shared.ResultPattern;

namespace Domain.Aggregates.Notifications.Errors
{
    public static class NotificationErrors
    {
        public static Error NotFound => Error.NotFound("Notification.NotFound", "The notification was not found.");
        public static Error NotOwnedByUser => Error.Forbidden("Notification.NotOwnedByUser", "The notification does not belong to the user.");
        public static Error InvalidType => Error.Failure("Notification.InvalidType", "Invalid notification type.");
        public static Error InvalidTarget => Error.Failure("Notification.InvalidTarget", "Invalid notification target.");
        public static Error TargetIdRequired => Error.Failure("Notification.TargetIdRequired", "A notification target requires a target id.");
        public static Error TitleTooLong => Error.Failure("Notification.TitleTooLong", $"The title cannot exceed {Notification.MaxTitleLength} characters.");
        public static Error MessageTooLong => Error.Failure("Notification.MessageTooLong", $"The message cannot exceed {Notification.MaxMessageLength} characters.");
    }
}
